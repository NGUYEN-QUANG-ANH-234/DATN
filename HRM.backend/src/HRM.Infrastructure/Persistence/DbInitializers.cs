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
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
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
            new Role { RoleName = "Director", Description = "Ban giám đốc" },
            new Role { RoleName = "Admin", Description = "Quản trị hệ thống" },
            new Role { RoleName = "HR", Description = "Nhân sự" },
            new Role { RoleName = "Manager", Description = "Quản lý phòng ban" },
            new Role { RoleName = "Employee", Description = "Nhân viên chính thức" },
        };
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        var departments = new List<Department>
        {
            new Department { DeptCode = "TECH", DeptName = "Phòng Kỹ thuật", Status = DeptStatus.Active },
            new Department { DeptCode = "SALE", DeptName = "Phòng Kinh doanh", Status = DeptStatus.Active },
            new Department { DeptCode = "HR", DeptName = "Phòng Nhân sự", Status = DeptStatus.Active },
            new Department { DeptCode = "ACC", DeptName = "Phòng Kế toán", Status = DeptStatus.Active }
        };
        context.Departments.AddRange(departments);

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
            new LeaveType { TypeName = "Phép năm", IsPaid = true },
            new LeaveType { TypeName = "Nghỉ ốm", IsPaid = true },
            new LeaveType { TypeName = "Nghỉ không lương", IsPaid = false }
        };
        context.LeaveTypes.AddRange(leaveTypes);

        var allowanceTypes = new List<AllowanceType>
        {
            new AllowanceType { TypeName = "Phụ cấp ăn trưa", IsTaxable = false, IsInsuranceBase = false },
            new AllowanceType { TypeName = "Phụ cấp đi lại", IsTaxable = true, IsInsuranceBase = false }
        };
        context.AllowanceTypes.AddRange(allowanceTypes);

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
                IsIntern = faker.Random.Bool(0.2f),
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
        // MODULE 6: TASKS & BUDGET (Công việc & Thưởng)
        // ======================================================
        var budget = new DepartmentBudget
        {
            DeptId = departments[0].Id,
            MonthYear = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            TotalBudget = 50000000,
            UsedBudget = 10000000,
            Status = BudgetStatus.Approved
        };
        context.DepartmentBudgets.Add(budget);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Tasks.Add(new WorkTask
            {
                Title = $"Task dự án {faker.Company.CompanyName()}",
                TaskType = TaskType.Project,
                AssignedTo = employees[i].Id,
                DeptBudgetId = budget.Id,
                BonusAmount = faker.Random.Decimal(1000000, 3000000),
                ActualBonus = 0,
                Status = TaskStatus.Doing,
                Deadline = DateTime.Now.AddDays(7)
            });
        }
        await context.SaveChangesAsync();

        // ======================================================
        // MODULE 7: PAYROLL (Tính lương nháp)
        // ======================================================
        context.PayrollFormulas.Add(new PayrollFormula
        {
            FormulaName = "Lương cơ bản 2024",
            Expression = "Gross = BasicSalary * (WorkDays/StandardDays) + TotalAllowance",
            Status = FormulaStatus.Approved
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
        // MODULE 8: REQUESTS & HANDOVER (Biến động nhân sự)
        // ======================================================
        var resignationRequest = new Request
        {
            EmployeeId = employees[10].Id,
            RequestType = RequestType.Resignation,
            Content = "Xin nghỉ việc vì lý do cá nhân",
            Status = RequestStatus.Pending,
            DeadlineAt = DateTime.Now.AddDays(2)
        };
        context.Requests.Add(resignationRequest);
        await context.SaveChangesAsync();

        context.HandoverRequests.Add(new HandoverRequest
        {
            RequestId = resignationRequest.Id,
            SenderId = employees[10].Id,
            ReceiverId = employees[11].Id, // Bàn giao cho người khác
            Status = HandoverStatus.Pending
        });

        await context.SaveChangesAsync();
    }
}