using Bogus;
using Microsoft.EntityFrameworkCore;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Entities;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

public static class DbInitializer
{
    public static async Task SeedData(MyDbContext context)
    {
        // Nếu đã có Account, tức là đã seed -> bỏ qua
        if (await context.Accounts.AnyAsync()) return;

        var faker = new Faker("vi");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        // ======================================================
        // MODULE 1 & 2: HỆ THỐNG & TỔ CHỨC CỐ ĐỊNH
        // ======================================================
        var roles = new List<Role>
        {
            new Role { Id = 1, RoleName = "Admin", Description = "Quản trị hệ thống" },
            new Role { Id = 2, RoleName = "Director", Description = "Ban giám đốc" },            
            new Role { Id = 3, RoleName = "HR", Description = "Nhân sự" },
            new Role { Id = 4, RoleName = "Manager", Description = "Quản lý phòng ban" },
            new Role { Id = 5, RoleName = "Employee", Description = "Nhân viên chính thức" },
            new Role { Id = 6, RoleName = "Collaborator", Description = "Cộng tác viên" },
            new Role { Id = 7, RoleName = "Intern", Description = "Thực tập sinh" },
            new Role { Id = 8, RoleName = "Candidate", Description = "Ứng viên" },
            
        };
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        var departments = (await HicasDepartmentSeeder.SyncAsync(context)).ToList();

        var positions = new List<Position>
        {
            new Position { Title = "Senior Developer", JobLevel = 4 },
            new Position { Title = "Junior Developer", JobLevel = 2 },
            new Position { Title = "HR Specialist", JobLevel = 3 },
            new Position { Title = "Sales Executive", JobLevel = 2 }
        };
        context.Positions.AddRange(positions);

        var workShifts = new List<WorkShift>
        {
            new WorkShift { ShiftName = "Hành chính", StartTime = new TimeSpan(8,0,0), EndTime = new TimeSpan(17,30,0) }
        };
        context.WorkShifts.AddRange(workShifts);

        var leaveTypes = new List<LeaveType>
        {
            new LeaveType
            {
                TypeName = "Phép năm",
                IsPaid = true,
                Category = LeaveCategory.AnnualPaid,
                CountsAsWorkday = true,
                DeductAnnualLeave = true,
                AffectsKpiPenalty = false
            },
            new LeaveType
            {
                TypeName = "Nghỉ ốm",
                IsPaid = true,
                Category = LeaveCategory.Sick,
                CountsAsWorkday = false,
                DeductAnnualLeave = false,
                AffectsKpiPenalty = false
            },
            new LeaveType
            {
                TypeName = "Nghỉ không lương",
                IsPaid = false,
                Category = LeaveCategory.Unpaid,
                CountsAsUnpaidForInsurance = true,
                CountsAsWorkday = false,
                DeductAnnualLeave = false,
                AffectsKpiPenalty = false
            },
            new LeaveType
            {
                TypeName = "Nghỉ thai sản",
                IsPaid = false,
                Category = LeaveCategory.Maternity,
                CountsAsUnpaidForInsurance = false,
                CountsAsWorkday = false,
                DeductAnnualLeave = false,
                AffectsKpiPenalty = false
            }
        };
        context.LeaveTypes.AddRange(leaveTypes);

        var allowanceTypes = new List<AllowanceType>
        {
            new AllowanceType { TypeName = "Phụ cấp ăn trưa", IsTaxable = false, IsInsuranceBase = false },
            new AllowanceType { TypeName = "Phụ cấp đi lại", IsTaxable = true, IsInsuranceBase = false }
        };
        context.AllowanceTypes.AddRange(allowanceTypes);

        var penaltyRules = new List<PenaltyRule>
        {
            new PenaltyRule
            {
                SourceType = PenaltySourceType.Attendance,
                RuleCode = "ATTENDANCE_LATE_MONTHLY",
                RuleName = "Di muon vuot nguong trong ky",
                Description = "Dung cho worker tong hop cham cong khi so lan/phut di muon vuot nguong cau hinh.",
                ThresholdValue = 3,
                ThresholdUnit = "times/month",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                AffectsPersonnelDecision = true,
                Severity = PenaltySeverity.Medium,
                RequiresEmployeeExplanation = true,
                RequiresHRApproval = true
            },
            new PenaltyRule
            {
                SourceType = PenaltySourceType.Leave,
                RuleCode = "LEAVE_OVER_BALANCE",
                RuleName = "Nghi vuot quy phep",
                Description = "Dùng cho worker nghỉ phép khi nhân viên nghỉ vượt quỹ phép được cấp.",
                ThresholdValue = 0,
                ThresholdUnit = "day",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                AffectsPersonnelDecision = true,
                Severity = PenaltySeverity.Medium,
                RequiresEmployeeExplanation = true,
                RequiresHRApproval = true
            },
            new PenaltyRule
            {
                SourceType = PenaltySourceType.SLA,
                RuleCode = "SLA_APPROVAL_VIOLATION",
                RuleName = "Cham xu ly phe duyet SLA",
                Description = "Dùng cho các workflow có SLA khi người xử lý dưới cấp Giám đốc bị quá hạn.",
                ThresholdValue = 1,
                ThresholdUnit = "violation",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                Severity = PenaltySeverity.Low,
                RequiresHRApproval = false
            },
            new PenaltyRule
            {
                SourceType = PenaltySourceType.Task,
                RuleCode = "TASK_SUBMISSION_OVERDUE",
                RuleName = "Tre han nop cong viec",
                Description = "Dùng cho TaskSlaWorker khi nhân viên trễ hạn nộp tiến độ/minh chứng.",
                ThresholdValue = 1,
                ThresholdUnit = "violation",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                Severity = PenaltySeverity.Low,
                RequiresHRApproval = false
            },
            new PenaltyRule
            {
                SourceType = PenaltySourceType.SLA,
                RuleCode = "TASK_REVIEW_SLA_VIOLATION",
                RuleName = "Cham duyet cong viec",
                Description = "Dùng cho TaskSlaWorker khi Trưởng phòng chậm duyệt task.",
                ThresholdValue = 1,
                ThresholdUnit = "violation",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                Severity = PenaltySeverity.Low,
                RequiresHRApproval = false
            },
            new PenaltyRule
            {
                SourceType = PenaltySourceType.SLA,
                RuleCode = "TRAINING_EVAL_SLA_VIOLATION",
                RuleName = "Cham danh gia dao tao",
                Description = "Dùng cho TrainingSlaWorker khi Trưởng phòng chậm đánh giá thực tập sinh/nhân sự đào tạo.",
                ThresholdValue = 1,
                ThresholdUnit = "violation",
                PenaltyPoint = 1,
                AffectsPerformance = true,
                Severity = PenaltySeverity.Low,
                RequiresHRApproval = false
            }
        };
        context.PenaltyRules.AddRange(penaltyRules);

        await context.SaveChangesAsync();

        // ======================================================
        // MODULE 3 & 4: TÀI KHOẢN & HỒ SƠ NHÂN SỰ
        // ======================================================
        var employees = new List<Employee>();
        var employeeRoleId = roles.First(r => r.RoleName == "Employee").Id;

        for (int i = 1; i <= 20; i++) // Tạo 20 nhân viên
        {
            var acc = new Account
            {
                Email = $"nv{i}@hicas.com",
                PasswordHash = passwordHash,
                RoleId = employeeRoleId,
                Status = AccountStatus.Active
            };
            context.Accounts.Add(acc);
            await context.SaveChangesAsync(); // Lấy AccountId

            var emp = new Employee
            {
                AccountId = acc.Id,
                EmployeeCode = $"NV{i:D4}",
                FullName = faker.Name.FullName(),
                Gender = faker.PickRandom<Gender>(),
                BirthDate = faker.Date.Past(30, DateTime.Now.AddYears(-22)),
                IdentityNumber = faker.Random.Replace("00109#########"),
                TaxCode = faker.Random.Replace("8########"),
                BankAccount = faker.Finance.Account(10),
                BankName = "Vietcombank",
                DeptId = faker.PickRandom(departments).Id,
                PositionId = faker.PickRandom(positions).Id,
                //IsIntern = faker.Random.Bool(0.2f),
                Status = EmployeeStatus.Official,
                JoinedDate = faker.Date.Past(2)
            };
            context.Employees.Add(emp);
            employees.Add(emp);
        }
        await context.SaveChangesAsync();

        // ======================================================
        // MODULE 5: HỢP ĐỒNG, PHỤ CẤP, PHÉP & GIA ĐÌNH
        // ======================================================
        foreach (var emp in employees)
        {
            // 1. Hợp đồng
            context.Contracts.Add(new Contract
            {
                EmployeeId = emp.Id,
                ContractNumber = $"HD-{emp.EmployeeCode}",
                ContractType = ContractType.Definite,
                BasicSalary = faker.Random.Decimal(10000000, 25000000),
                SalaryPercentage = 100,
                StartDate = emp.JoinedDate!.Value,
                EndDate = emp.JoinedDate.Value.AddYears(1),
                Status = ContractStatus.Active
            });

            // 2. Phụ cấp cố định (EmployeeAllowances)
            context.EmployeeAllowances.Add(new EmployeeAllowance
            {
                EmployeeId = emp.Id,
                AllowanceTypeId = allowanceTypes[0].Id, // Ăn trưa
                Amount = 730000
            });

            // 3. Quỹ phép (LeaveBalance)
            context.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId = emp.Id,
                LeaveTypeId = leaveTypes[0].Id, // Phép năm
                Year = (short)DateTime.Now.Year,
                TotalDays = 12,
                UsedDays = faker.Random.Decimal(1, 5)
            });

            // 4. Chấm công 10 ngày gần nhất
            for (int d = 1; d <= 10; d++)
            {
                context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = emp.Id,
                    ShiftId = workShifts[0].Id,
                    WorkDate = DateTime.Now.Date.AddDays(-d),
                    CheckIn = DateTime.Now.Date.AddDays(-d).AddHours(8).AddMinutes(faker.Random.Int(-10, 15)),
                    CheckOut = DateTime.Now.Date.AddDays(-d).AddHours(17).AddMinutes(faker.Random.Int(30, 60)),
                    IpAddress = "192.168.1.100",
                    // 21.004118 → Latitude(vĩ độ)
                    // 105.843381 → Longitude(kinh độ)
                    //GpsLocation = "21.004118,105.843381", // Tọa độ HUST
                    GpsLat = 21.004118M,
                    GpsLong = 105.843381M,
                    Status = AttendanceStatus.Valid
                });
            }
        }
        await context.SaveChangesAsync();

        // ======================================================
        // MODULE 6: PERFORMANCE & TRAINING (Hiệu suất & Đào tạo)
        // ======================================================

        var kpiNames = new[] { "Hoàn thành dự án đúng hạn", "Chất lượng công việc", "Thái độ & Kỷ luật" };

        for (int i = 0; i < 5; i++)
        {
            // 1. Tạo Phiếu đánh giá KPI tổng
            var review = new PerformanceReview
            {
                EmployeeId = employees[i].Id,
                Period = $"{DateTime.Now.Month:D2}/{DateTime.Now.Year}", // VD: "05/2026"
                TotalScore = faker.Random.Decimal(75, 100),
                ScoringVersion = "Legacy",
                FinalRating = faker.PickRandom(new[] { "A", "B", "C" }),
                Status = ReviewStatus.Approved
            };
            context.PerformanceReviews.Add(review);
            await context.SaveChangesAsync(); // Lưu để lấy Id

            // 2. Tạo các dòng chi tiết KPI
            foreach (var kpi in kpiNames)
            {
                context.PerformanceDetails.Add(new PerformanceDetail
                {
                    ReviewId = review.Id,
                    KpiCode = $"KPI-{review.Id}-{Array.IndexOf(kpiNames, kpi) + 1}",
                    KpiName = kpi,
                    WeightPercent = 33,
                    AchievedPercent = faker.Random.Decimal(80, 100),
                    FinalPoint = faker.Random.Decimal(25, 33)
                });
            }
        }

        await context.SaveChangesAsync();

        // ======================================================
        // MODULE 7: PAYROLL (Tính lương nháp)
        // ======================================================
        context.PayrollFormulas.Add(new PayrollFormula
        {
            FormulaCode = "DEFAULT_PAYROLL_V2",
            FormulaName = "Công thức lương mặc định",
            Expression = "gross_income = sum(payroll_formula_lines)",
            Status = FormulaStatus.Approved,
            IsActive = true,
            Version = 1,
            VersionCode = "LEGACY_KPI_TARGET_V1",
            EffectiveFrom = new DateTime(2020, 7, 1),
            ApprovedAt = DateTime.UtcNow,
            Lines = new List<PayrollFormulaLine>
            {
                new PayrollFormulaLine { ComponentCode = "BASE_SALARY_ACTUAL", Expression = "contract_segment_salary_amount", CalculationOrder = 10, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "POSITION_ALLOWANCE", Expression = "position_allowance / standard_workdays * actual_workdays", CalculationOrder = 20, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "RESPONSIBILITY_ALLOWANCE", Expression = "responsibility_allowance / standard_workdays * actual_workdays", CalculationOrder = 30, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "SENIORITY_ALLOWANCE", Expression = "seniority_allowance_prorated", CalculationOrder = 35, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "MEAL_ALLOWANCE", Expression = "meal_allowance_per_day * actual_attendance_days", CalculationOrder = 40, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_INSURANCE_ALLOWANCE", Expression = "legacy_insurance_allowance", CalculationOrder = 50, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_TAXABLE_ALLOWANCE", Expression = "legacy_taxable_allowance", CalculationOrder = 60, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_NONTAXABLE_ALLOWANCE", Expression = "legacy_nontaxable_allowance", CalculationOrder = 70, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "INTERN_ALLOWANCE", Expression = "intern_allowance_amount", CalculationOrder = 75, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "KPI_BONUS", Expression = "kpi_bonus_amount", CalculationOrder = 80, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PROJECT_BONUS", Expression = "project_bonus_amount", CalculationOrder = 87, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "OT_BASE", Expression = "overtime_base_amount", CalculationOrder = 90, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "OT_PREMIUM", Expression = "overtime_premium_amount", CalculationOrder = 100, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE", Expression = "payroll_adjustment_taxable_insurance", CalculationOrder = 110, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_TAXABLE", Expression = "payroll_adjustment_taxable", CalculationOrder = 120, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_NONTAXABLE", Expression = "payroll_adjustment_nontaxable", CalculationOrder = 130, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "EMPLOYEE_INSURANCE", Expression = "insurance_salary * employee_insurance_rate", CalculationOrder = 200, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PIT", Expression = "pit(pit_tax_base)", CalculationOrder = 210, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_DEDUCTION", Expression = "payroll_adjustment_deduction", CalculationOrder = 220, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true }
            }
        });

        context.PayrollFormulas.Add(new PayrollFormula
        {
            FormulaCode = "DEFAULT_PAYROLL_V2",
            FormulaName = "Công thức lương mặc định - KPI theo điểm",
            Expression = "gross_income = sum(payroll_formula_lines)",
            Status = FormulaStatus.Approved,
            IsActive = true,
            Version = 2,
            VersionCode = "KPI_PAYOUT_V2",
            EffectiveFrom = new DateTime(2026, 6, 1),
            ApprovedAt = DateTime.UtcNow,
            Lines = new List<PayrollFormulaLine>
            {
                new PayrollFormulaLine { ComponentCode = "BASE_SALARY_ACTUAL", Expression = "contract_segment_salary_amount", CalculationOrder = 10, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "POSITION_ALLOWANCE", Expression = "position_allowance / standard_workdays * actual_workdays", CalculationOrder = 20, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "RESPONSIBILITY_ALLOWANCE", Expression = "responsibility_allowance / standard_workdays * actual_workdays", CalculationOrder = 30, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "SENIORITY_ALLOWANCE", Expression = "seniority_allowance_prorated", CalculationOrder = 35, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "MEAL_ALLOWANCE", Expression = "meal_allowance_per_day * actual_attendance_days", CalculationOrder = 40, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_INSURANCE_ALLOWANCE", Expression = "legacy_insurance_allowance", CalculationOrder = 50, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_TAXABLE_ALLOWANCE", Expression = "legacy_taxable_allowance", CalculationOrder = 60, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "LEGACY_NONTAXABLE_ALLOWANCE", Expression = "legacy_nontaxable_allowance", CalculationOrder = 70, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "INTERN_ALLOWANCE", Expression = "intern_allowance_amount", CalculationOrder = 75, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "KPI_BONUS", Expression = "kpi_bonus_amount * kpi_score / 100", CalculationOrder = 80, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true, Note = "Khoản thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100." },
                new PayrollFormulaLine { ComponentCode = "PROJECT_BONUS", Expression = "project_bonus_amount", CalculationOrder = 87, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "OT_BASE", Expression = "overtime_base_amount", CalculationOrder = 90, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "OT_PREMIUM", Expression = "overtime_premium_amount", CalculationOrder = 100, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE", Expression = "payroll_adjustment_taxable_insurance", CalculationOrder = 110, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = true, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_TAXABLE", Expression = "payroll_adjustment_taxable", CalculationOrder = 120, IsGrossComponent = true, IsTaxable = true, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_NONTAXABLE", Expression = "payroll_adjustment_nontaxable", CalculationOrder = 130, IsGrossComponent = true, IsTaxable = false, IsInsuranceBased = false, IsDeduction = false, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "EMPLOYEE_INSURANCE", Expression = "insurance_salary * employee_insurance_rate", CalculationOrder = 200, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PIT", Expression = "pit(pit_tax_base)", CalculationOrder = 210, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true },
                new PayrollFormulaLine { ComponentCode = "PAYROLL_ADJUSTMENT_DEDUCTION", Expression = "payroll_adjustment_deduction", CalculationOrder = 220, IsGrossComponent = false, IsTaxable = false, IsInsuranceBased = false, IsDeduction = true, IsSnapshotRequired = true }
            }
        });

        foreach (var emp in employees.Take(5)) // Lấy 5 người tạo phiếu lương tháng trước
        {
            context.Payrolls.Add(new Payroll
            {
                EmployeeId = emp.Id,
                Month = (byte)DateTime.Now.AddMonths(-1).Month,
                Year = (short)DateTime.Now.Year,
                GrossSalary = 15000000,
                TotalAllowance = 730000,
                TotalBonus = 0,
                InsuranceDeduction = 1575000, // 10.5%
                TaxableIncome = 13000000,
                PitAmount = 250000,
                NetSalary = 13905000,
                Status = PayrollStatus.Finalized
            });
        }

        // ======================================================
        // MODULE 8: REQUESTS (Biến động nhân sự)
        // ======================================================
        var resignationRequest = new Request
        {
            EmployeeId = employees[10].Id,
            RequestType = RequestType.Resignation,
            Content = "Xin nghỉ việc vì lý do cá nhân",
            Status = RequestStatus.Pending_Manager,
            DeadlineAt = DateTime.Now.AddDays(2)
        };
        context.Requests.Add(resignationRequest);
        await context.SaveChangesAsync();
    }
}
