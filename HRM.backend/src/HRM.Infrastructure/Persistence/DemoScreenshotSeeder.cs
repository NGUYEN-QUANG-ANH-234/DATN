using System.Text.Json;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppConfig = HRM.backend.src.HRM.Core.Entities.System.Configuration;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Infrastructure.Persistence
{
    public static class DemoScreenshotSeeder
    {
        private const string MarkerGroup = "DEMO_SCREENSHOT_SEED";
        private const string MarkerKey = "V20260621_WORKFLOW_BASELINE_RESET";
        private const string JulyPayrollSandboxMarkerKey = "V20260622_JULY_PAYROLL_SANDBOX";
        private const string DemoPassword = "123456";
        private const decimal InternMonthlyAllowance = 2000000m;

        public static async Task AutoSyncAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            CancellationToken ct = default)
        {
            var enabled = configuration.GetValue<bool>("Seed:DemoScreenshotData:Enabled") ||
                          configuration.GetValue<bool>("Seed:DemoData:Enabled");

            if (!enabled) return;

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                var baselineSeeded = await context.Configurations.AnyAsync(
                        c => c.ConfigGroup == MarkerGroup && c.ParamKey == MarkerKey && c.IsActive,
                        ct);
                var julyPayrollSeeded = await context.Configurations.AnyAsync(
                        c => c.ConfigGroup == MarkerGroup && c.ParamKey == JulyPayrollSandboxMarkerKey && c.IsActive,
                        ct);

                if (baselineSeeded && !julyPayrollSeeded)
                {
                    await HicasDepartmentSeeder.SyncAsync(context, ct);

                    await using var julyTransaction = await context.Database.BeginTransactionAsync(ct);

                    var julyRoles = await EnsureRolesAsync(context, ct);
                    var julyOrg = await EnsureOrganizationAsync(context, ct);
                    var julyDemo = await EnsureAccountsAndEmployeesAsync(context, julyRoles, julyOrg, ct);

                    await EnsureJulyPayrollSandboxDataAsync(context, julyDemo, julyOrg, ct);
                    await AddSeedMarkerAsync(context, JulyPayrollSandboxMarkerKey, "Seed July 2026 attendance/payroll sandbox input data.", ct);

                    await context.SaveChangesAsync(ct);
                    await julyTransaction.CommitAsync(ct);
                    return;
                }

                if (baselineSeeded)
                {
                    return;
                }

                await HicasDepartmentSeeder.SyncAsync(context, ct);

                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                var roles = await EnsureRolesAsync(context, ct);
                var org = await EnsureOrganizationAsync(context, ct);
                var demo = await EnsureAccountsAndEmployeesAsync(context, roles, org, ct);

                await ResetDemoWorkflowDataAsync(context, demo, ct);

                await EnsureModule1DataAsync(context, demo, ct);
                await EnsureModule2RecruitmentAsync(context, demo, org, ct);
                await EnsureModule3ProfileAndContractsAsync(context, demo, org, ct);
                await EnsureModule4AttendanceAsync(context, demo, org, ct);
                await EnsureModule5PerformanceAsync(context, demo, org, ct);
                await EnsureModule6PayrollAsync(context, demo, ct);
                await EnsureModule7PersonnelChangesAsync(context, demo, org, ct);
                await EnsureReferenceCoverageAsync(context, demo, org, ct);
                await EnsureJulyPayrollSandboxDataAsync(context, demo, org, ct);

                await AddSeedMarkerAsync(context, MarkerKey, "Baseline demo data with workflow records reset to their first actionable step.", ct);
                await AddSeedMarkerAsync(context, JulyPayrollSandboxMarkerKey, "Seed July 2026 attendance/payroll sandbox input data.", ct);

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            });
        }

        private static async Task<Dictionary<string, Role>> EnsureRolesAsync(MyDbContext context, CancellationToken ct)
        {
            var roleSeeds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Admin"] = "Quan tri he thong",
                ["Director"] = "Ban giam doc",
                ["HR"] = "Nhan su",
                ["Manager"] = "Truong phong",
                ["Employee"] = "Nhan vien",
                ["Collaborator"] = "Cong tac vien",
                ["Intern"] = "Thuc tap sinh",
                ["Candidate"] = "Ung vien"
            };

            foreach (var seed in roleSeeds)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == seed.Key, ct);
                if (role == null)
                {
                    context.Roles.Add(new Role { RoleName = seed.Key, Description = seed.Value });
                }
                else if (string.IsNullOrWhiteSpace(role.Description))
                {
                    role.Description = seed.Value;
                }
            }

            await context.SaveChangesAsync(ct);

            return await context.Roles
                .Where(r => roleSeeds.Keys.Contains(r.RoleName))
                .ToDictionaryAsync(r => r.RoleName, StringComparer.OrdinalIgnoreCase, ct);
        }

        private sealed record DemoOrg(
            Department Bod,
            Department Hr,
            Department Tech,
            Department Product,
            Department Sales,
            Position DirectorPosition,
            Position HrPosition,
            Position ManagerPosition,
            Position EngineerPosition,
            Position AnalystPosition,
            Position InternPosition,
            JobLevel InternLevel,
            JobLevel StaffLevel,
            JobLevel SeniorLevel,
            JobLevel ManagerLevel,
            JobLevel DirectorLevel);

        private static async Task<DemoOrg> EnsureOrganizationAsync(MyDbContext context, CancellationToken ct)
        {
            var bod = await EnsureDepartmentAsync(context, "BOD", "Ban Giam doc", ct);
            var hr = await EnsureDepartmentAsync(context, "HR", "Phong Nhan su", ct);
            var tech = await EnsureDepartmentAsync(context, "TECH", "Khoi Ky thuat phan mem", ct);
            var product = await EnsureDepartmentAsync(context, "PRODUCT", "Phong San pham", ct);
            var sales = await EnsureDepartmentAsync(context, "SALE", "Phong Kinh doanh va Marketing", ct);

            var director = await EnsurePositionAsync(context, "Demo Giam doc dieu hanh", 8, ct);
            var hrPos = await EnsurePositionAsync(context, "Demo Chuyen vien nhan su", 3, ct);
            var managerPos = await EnsurePositionAsync(context, "Demo Truong phong ky thuat", 6, ct);
            var engineer = await EnsurePositionAsync(context, "Demo Ky su phan mem", 4, ct);
            var analyst = await EnsurePositionAsync(context, "Demo Chuyen vien phan tich du lieu", 4, ct);
            var internPos = await EnsurePositionAsync(context, "Demo Thuc tap sinh phan mem", 1, ct);

            var internLevel = await EnsureJobLevelAsync(context, "DEMO-INTERN", "Demo Thuc tap sinh", 1, false, ct);
            var staffLevel = await EnsureJobLevelAsync(context, "DEMO-L2", "Demo Nhan vien", 2, false, ct);
            var seniorLevel = await EnsureJobLevelAsync(context, "DEMO-L4", "Demo Chuyen vien cao cap", 4, false, ct);
            var managerLevel = await EnsureJobLevelAsync(context, "DEMO-M1", "Demo Quan ly", 6, true, ct);
            var directorLevel = await EnsureJobLevelAsync(context, "DEMO-D1", "Demo Giam doc", 8, true, ct);

            await context.SaveChangesAsync(ct);

            return new DemoOrg(
                bod, hr, tech, product, sales,
                director, hrPos, managerPos, engineer, analyst, internPos,
                internLevel, staffLevel, seniorLevel, managerLevel, directorLevel);
        }

        private sealed record DemoUsers(
            Account AdminAccount,
            Account DirectorAccount,
            Account HrAccount,
            Account ManagerAccount,
            Account EmployeeAccount,
            Account CollaboratorAccount,
            Account InternAccount,
            Employee Director,
            Employee Hr,
            Employee Manager,
            Employee Employee,
            Employee Collaborator,
            Employee Intern);

        private static async Task<DemoUsers> EnsureAccountsAndEmployeesAsync(
            MyDbContext context,
            Dictionary<string, Role> roles,
            DemoOrg org,
            CancellationToken ct)
        {
            var admin = await EnsureAccountAsync(context, "demo.admin@hicas.vn", "Demo Admin HICAS", roles["Admin"], ct);
            var directorAcc = await EnsureAccountAsync(context, "demo.director@hicas.vn", "Demo Director", roles["Director"], ct);
            var hrAcc = await EnsureAccountAsync(context, "demo.hr@hicas.vn", "Demo HR", roles["HR"], ct);
            var managerAcc = await EnsureAccountAsync(context, "demo.manager@hicas.vn", "Demo Manager", roles["Manager"], ct);
            var employeeAcc = await EnsureAccountAsync(context, "demo.employee@hicas.vn", "Demo Employee", roles["Employee"], ct);
            var collaboratorAcc = await EnsureAccountAsync(context, "demo.collaborator@hicas.vn", "Demo Collaborator", roles["Collaborator"], ct);
            var internAcc = await EnsureAccountAsync(context, "demo.intern@hicas.vn", "Demo Intern", roles["Intern"], ct);

            await context.SaveChangesAsync(ct);

            var director = await EnsureEmployeeAsync(context, directorAcc, "DEMO-DIR01", "Nguyen Minh Quan", org.Bod, org.DirectorPosition, org.DirectorLevel, null, EmployeeType.Official, EmployeeStatus.Official, ct);
            var hr = await EnsureEmployeeAsync(context, hrAcc, "DEMO-HR01", "Tran Ha Linh", org.Hr, org.HrPosition, org.SeniorLevel, director, EmployeeType.Official, EmployeeStatus.Official, ct);
            var manager = await EnsureEmployeeAsync(context, managerAcc, "DEMO-MAN01", "Le Quang Huy", org.Tech, org.ManagerPosition, org.ManagerLevel, director, EmployeeType.Official, EmployeeStatus.Official, ct);
            var employee = await EnsureEmployeeAsync(context, employeeAcc, "DEMO-EMP01", "Pham Anh Duong", org.Tech, org.EngineerPosition, org.StaffLevel, manager, EmployeeType.Official, EmployeeStatus.Official, ct);
            var collaborator = await EnsureEmployeeAsync(context, collaboratorAcc, "DEMO-COL01", "Do Mai Anh", org.Product, org.AnalystPosition, org.StaffLevel, manager, EmployeeType.Contractual, EmployeeStatus.Official, ct);
            var intern = await EnsureEmployeeAsync(context, internAcc, "DEMO-INT01", "Bui Khanh An", org.Tech, org.InternPosition, org.InternLevel, manager, EmployeeType.Intern, EmployeeStatus.Probation, ct);

            org.Bod.ManagerId = director.Id;
            org.Hr.ManagerId = hr.Id;
            org.Tech.ManagerId = manager.Id;
            org.Product.ManagerId = manager.Id;

            await context.SaveChangesAsync(ct);

            return new DemoUsers(
                admin, directorAcc, hrAcc, managerAcc, employeeAcc, collaboratorAcc, internAcc,
                director, hr, manager, employee, collaborator, intern);
        }

        private static async Task ResetDemoWorkflowDataAsync(MyDbContext context, DemoUsers demo, CancellationToken ct)
        {
            var demoEmployeeIds = new[]
            {
                demo.Director.Id,
                demo.Hr.Id,
                demo.Manager.Id,
                demo.Employee.Id,
                demo.Collaborator.Id,
                demo.Intern.Id
            };

            var demoWorkflowModules = new[]
            {
                "RECRUITMENT",
                "CANDIDATE",
                "PROFILE_UPDATE",
                "CONTRACT_DEPT",
                "CONTRACT_DIRECTOR",
                "LEAVE_REQUEST",
                "OVERTIME_MANAGER",
                "OVERTIME_DIRECTOR",
                "PAYROLL_RUN_APPROVAL",
                "PAYROLL_FORMULA_APPROVAL",
                "PROJECT_BONUS_IMPORT",
                "EXTERNAL_TIMESHEET_IMPORT",
                "PERSONNEL_CHANGE_APPROVAL",
                "PERFORMANCE_APPROVAL"
            };

            var demoApprovalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            static string ApprovalKey(string moduleCode, int referenceId) => $"{moduleCode}:{referenceId}";
            void TrackApproval(string moduleCode, int referenceId) => demoApprovalKeys.Add(ApprovalKey(moduleCode, referenceId));

            var approvalRequests = await context.ApprovalRequests
                .Include(a => a.Steps)
                .Where(a => demoWorkflowModules.Contains(a.ModuleCode))
                .ToListAsync(ct);

            var personnelChangeIds = await context.PersonnelChangeRequests
                .Where(p => p.Reason != null && p.Reason.StartsWith("Demo "))
                .Select(p => p.Id)
                .ToListAsync(ct);
            if (personnelChangeIds.Count > 0)
            {
                foreach (var id in personnelChangeIds)
                    TrackApproval("PERSONNEL_CHANGE_APPROVAL", id);

                context.PersonnelChangeApprovals.RemoveRange(await context.PersonnelChangeApprovals.Where(p => personnelChangeIds.Contains(p.RequestId)).ToListAsync(ct));
                context.PersonnelChangeHistories.RemoveRange(await context.PersonnelChangeHistories.Where(p => personnelChangeIds.Contains(p.RequestId)).ToListAsync(ct));
                context.PersonnelChangeContractLinks.RemoveRange(await context.PersonnelChangeContractLinks.Where(p => personnelChangeIds.Contains(p.PersonnelChangeRequestId)).ToListAsync(ct));
                context.PersonnelChangeRiskSnapshots.RemoveRange(await context.PersonnelChangeRiskSnapshots.Where(p => personnelChangeIds.Contains(p.RequestId)).ToListAsync(ct));
                context.PersonnelChangeRequests.RemoveRange(await context.PersonnelChangeRequests.Where(p => personnelChangeIds.Contains(p.Id)).ToListAsync(ct));
            }

            var projectBonusBatches = await context.ProjectBonusImportBatches
                .Include(b => b.Lines)
                .Where(b => b.FileName == "demo-project-bonus.xlsx")
                .ToListAsync(ct);
            if (projectBonusBatches.Count > 0)
            {
                foreach (var batch in projectBonusBatches)
                    TrackApproval("PROJECT_BONUS_IMPORT", batch.Id);

                context.ProjectBonusImportLines.RemoveRange(projectBonusBatches.SelectMany(b => b.Lines));
                context.ProjectBonusImportBatches.RemoveRange(projectBonusBatches);
            }

            var externalTimesheetImports = await context.ExternalTimesheetImports
                .Include(i => i.Lines)
                .Where(i => i.FileName == "demo-external-timesheet.csv")
                .ToListAsync(ct);
            if (externalTimesheetImports.Count > 0)
            {
                foreach (var import in externalTimesheetImports)
                    TrackApproval("EXTERNAL_TIMESHEET_IMPORT", import.Id);

                context.ExternalTimesheetLines.RemoveRange(externalTimesheetImports.SelectMany(i => i.Lines));
                context.ExternalTimesheetImports.RemoveRange(externalTimesheetImports);
            }

            var payrollFormula = await context.PayrollFormulas
                .Include(f => f.Lines)
                .FirstOrDefaultAsync(f => f.FormulaCode == "DEMO_PAYROLL_SCREENSHOT", ct);
            if (payrollFormula != null)
            {
                TrackApproval("PAYROLL_FORMULA_APPROVAL", payrollFormula.Id);
                context.PayrollFormulaLines.RemoveRange(payrollFormula.Lines);
                context.PayrollFormulas.Remove(payrollFormula);
            }

            var now = DateTime.UtcNow;
            var payrollPeriod = $"{now.Month:D2}-{now.Year}";
            var payrollIds = await context.Payrolls
                .Where(p => p.EmployeeId.HasValue && demoEmployeeIds.Contains(p.EmployeeId.Value) && p.Period == payrollPeriod)
                .Select(p => p.Id)
                .ToListAsync(ct);
            if (payrollIds.Count > 0)
            {
                foreach (var id in payrollIds)
                    TrackApproval("PAYROLL_RUN_APPROVAL", id);

                context.PayrollDetails.RemoveRange(await context.PayrollDetails.Where(d => payrollIds.Contains(d.PayrollId)).ToListAsync(ct));
                context.PayrollContractSegments.RemoveRange(await context.PayrollContractSegments.Where(s => s.PayrollId.HasValue && payrollIds.Contains(s.PayrollId.Value)).ToListAsync(ct));
                context.Payrolls.RemoveRange(await context.Payrolls.Where(p => payrollIds.Contains(p.Id)).ToListAsync(ct));
            }

            context.PayrollAdjustments.RemoveRange(await context.PayrollAdjustments
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) &&
                            a.RecognizedPayrollPeriod == payrollPeriod &&
                            a.Reason != null &&
                            a.Reason.Contains("Demo"))
                .ToListAsync(ct));

            var overtimeIds = await context.OvertimeRequests
                .Where(o => demoEmployeeIds.Contains(o.EmployeeId) && o.Reason != null && o.Reason.StartsWith("Demo "))
                .Select(o => o.Id)
                .ToListAsync(ct);
            if (overtimeIds.Count > 0)
            {
                foreach (var id in overtimeIds)
                {
                    TrackApproval("OVERTIME_MANAGER", id);
                    TrackApproval("OVERTIME_DIRECTOR", id);
                }

                context.OvertimeSegments.RemoveRange(await context.OvertimeSegments.Where(s => overtimeIds.Contains(s.OvertimeRequestId)).ToListAsync(ct));
                context.OvertimeRequests.RemoveRange(await context.OvertimeRequests.Where(o => overtimeIds.Contains(o.Id)).ToListAsync(ct));
            }

            var leaveRequests = await context.LeaveRequests
                .Where(l => l.EmployeeId.HasValue &&
                            demoEmployeeIds.Contains(l.EmployeeId.Value) &&
                            l.Reason != null &&
                            l.Reason.StartsWith("Demo "))
                .ToListAsync(ct);
            if (leaveRequests.Count > 0)
            {
                foreach (var leave in leaveRequests)
                    TrackApproval("LEAVE_REQUEST", leave.Id);

                context.LeaveRequests.RemoveRange(leaveRequests);
            }

            context.AttendanceSummaries.RemoveRange(await context.AttendanceSummaries
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) && a.Month == now.Month && a.Year == now.Year)
                .ToListAsync(ct));
            context.AttendanceDailySummaries.RemoveRange(await context.AttendanceDailySummaries
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) && a.WorkDate.Month == now.Month && a.WorkDate.Year == now.Year)
                .ToListAsync(ct));

            var profileUpdateRequests = await context.ProfileUpdateRequests
                .Where(p => demoEmployeeIds.Contains(p.EmployeeId) && p.RequestedDataJson.Contains("0912345678"))
                .ToListAsync(ct);
            if (profileUpdateRequests.Count > 0)
            {
                foreach (var request in profileUpdateRequests)
                    TrackApproval("PROFILE_UPDATE", request.Id);

                context.ProfileUpdateRequests.RemoveRange(profileUpdateRequests);
            }

            var workflowContractNumbers = new[] { "DEMO-HD-INT01-2026", "DEMO-HD-MAN01-2026" };
            var workflowContractIds = await context.Contracts
                .Where(c => workflowContractNumbers.Contains(c.ContractNumber))
                .Select(c => c.Id)
                .ToListAsync(ct);

            var demoContractAddendums = await context.ContractAddendums
                .Include(a => a.Details)
                .Where(a => a.AddendumNumber.StartsWith("DEMO-PL-") || workflowContractIds.Contains(a.ContractId))
                .ToListAsync(ct);
            if (demoContractAddendums.Count > 0)
            {
                context.ContractAddendumDetails.RemoveRange(demoContractAddendums.SelectMany(a => a.Details));
                context.ContractAddendums.RemoveRange(demoContractAddendums);
            }

            if (workflowContractIds.Count > 0)
            {
                foreach (var id in workflowContractIds)
                {
                    TrackApproval("CONTRACT_DEPT", id);
                    TrackApproval("CONTRACT_DIRECTOR", id);
                }

                context.ContractLegalSnapshots.RemoveRange(await context.ContractLegalSnapshots.Where(s => workflowContractIds.Contains(s.ContractId)).ToListAsync(ct));
                context.PayrollContractSegments.RemoveRange(await context.PayrollContractSegments.Where(s => workflowContractIds.Contains(s.ContractId)).ToListAsync(ct));
                context.Contracts.RemoveRange(await context.Contracts.Where(c => workflowContractIds.Contains(c.Id)).ToListAsync(ct));
            }

            var demoRecruitmentIds = await context.RecruitmentRequests
                .Where(r => (r.Description != null && (
                                r.Description.Contains("du an ERP") ||
                                r.Description.Contains("phan tich du lieu san pham") ||
                                r.Description.Contains("da dong vi da du chi tieu"))))
                .Select(r => r.Id)
                .ToListAsync(ct);

            var demoCandidateIds = await context.Candidates
                .Where(c => (c.TrackingCode != null && c.TrackingCode.StartsWith("TRK-DEMO")) ||
                            (c.RecruitmentRequestId.HasValue && demoRecruitmentIds.Contains(c.RecruitmentRequestId.Value)))
                .Select(c => c.Id)
                .ToListAsync(ct);
            if (demoCandidateIds.Count > 0)
            {
                foreach (var id in demoCandidateIds)
                    TrackApproval("CANDIDATE", id);

                await context.OnboardingRequests
                    .Where(o => demoCandidateIds.Contains(o.CandidateId))
                    .ExecuteDeleteAsync(ct);
                await context.Employees
                    .Where(e => e.CandidateId.HasValue && demoCandidateIds.Contains(e.CandidateId.Value))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.CandidateId, (int?)null), ct);
                await context.Candidates
                    .Where(c => demoCandidateIds.Contains(c.Id))
                    .ExecuteDeleteAsync(ct);
            }

            if (demoRecruitmentIds.Count > 0)
            {
                foreach (var id in demoRecruitmentIds)
                    TrackApproval("RECRUITMENT", id);

                context.RecruitmentRequests.RemoveRange(await context.RecruitmentRequests.Where(r => demoRecruitmentIds.Contains(r.Id)).ToListAsync(ct));
            }

            var demoTaskIds = await context.Tasks
                .Where(t => t.Title.StartsWith("Demo Task:"))
                .Select(t => t.Id)
                .ToListAsync(ct);
            if (demoTaskIds.Count > 0)
            {
                context.TaskProgresses.RemoveRange(await context.TaskProgresses.Where(p => demoTaskIds.Contains(p.TaskId)).ToListAsync(ct));
                context.TaskFeedbacks.RemoveRange(await context.TaskFeedbacks.Where(f => f.TaskId.HasValue && demoTaskIds.Contains(f.TaskId.Value)).ToListAsync(ct));
                context.Tasks.RemoveRange(await context.Tasks.Where(t => demoTaskIds.Contains(t.Id)).ToListAsync(ct));
            }

            var demoTrainingIds = await context.Trainings
                .Where(t => t.CourseName == "Demo Onboarding HICAS")
                .Select(t => t.Id)
                .ToListAsync(ct);
            if (demoTrainingIds.Count > 0)
            {
                context.Trainings.RemoveRange(await context.Trainings.Where(t => demoTrainingIds.Contains(t.Id)).ToListAsync(ct));
            }

            context.PenaltyRecords.RemoveRange(await context.PenaltyRecords
                .Where(p => p.RuleCode == "DEMO_POLICY_VIOLATION")
                .ToListAsync(ct));

            var demoKpiBatchIds = await context.KpiImportBatches
                .Where(b => b.FileName == "demo-kpi-import.xlsx")
                .Select(b => b.Id)
                .ToListAsync(ct);
            var demoReviewIds = await context.PerformanceReviews
                .Where(r => demoEmployeeIds.Contains(r.EmployeeId) &&
                            (demoKpiBatchIds.Contains(r.ImportBatchId ?? 0) || r.Period == $"{now.Month:D2}/{now.Year}"))
                .Select(r => r.Id)
                .ToListAsync(ct);
            if (demoReviewIds.Count > 0)
            {
                foreach (var id in demoReviewIds)
                    TrackApproval("PERFORMANCE_APPROVAL", id);

                context.PerformanceDetails.RemoveRange(await context.PerformanceDetails.Where(d => demoReviewIds.Contains(d.ReviewId)).ToListAsync(ct));
                context.PerformanceReviews.RemoveRange(await context.PerformanceReviews.Where(r => demoReviewIds.Contains(r.Id)).ToListAsync(ct));
            }
            if (demoKpiBatchIds.Count > 0)
            {
                context.KpiImportBatches.RemoveRange(await context.KpiImportBatches.Where(b => demoKpiBatchIds.Contains(b.Id)).ToListAsync(ct));
            }

            var approvalRequestsToRemove = approvalRequests
                .Where(a => demoApprovalKeys.Contains(ApprovalKey(a.ModuleCode, a.ReferenceId)))
                .ToList();
            if (approvalRequestsToRemove.Count > 0)
            {
                context.ApprovalSteps.RemoveRange(approvalRequestsToRemove.SelectMany(a => a.Steps));
                context.ApprovalRequests.RemoveRange(approvalRequestsToRemove);
            }

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule1DataAsync(MyDbContext context, DemoUsers demo, CancellationToken ct)
        {
            if (!await context.AuditLogs.AnyAsync(a => a.ActionType == "DEMO_SCREENSHOT_AUDIT", ct))
            {
                context.AuditLogs.AddRange(
                    Audit(demo.AdminAccount.Id, "DEMO_SCREENSHOT_AUDIT", "accounts", null, "{\"email\":\"demo.employee@hicas.vn\"}", "Create demo account"),
                    Audit(demo.HrAccount.Id, "PROFILE_UPDATE_SUBMITTED", "employee_profile", null, "{\"phoneNumber\":\"0909000001\"}", "Employee submitted profile update"),
                    Audit(demo.ManagerAccount.Id, "ATTENDANCE_PERIOD_LOCKED", "attendance_summaries", "{\"status\":\"Approved\"}", "{\"status\":\"Locked\"}", "Monthly attendance locked"),
                    Audit(demo.HrAccount.Id, "PAYROLL_RUN_SUBMITTED", "payrolls", "{\"status\":\"Calculated\"}", "{\"status\":\"PendingApproval\"}", "Payroll run submitted"));
            }

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule2RecruitmentAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            await EnsureRecruitmentAsync(
                context,
                org.Tech,
                org.EngineerPosition,
                3,
                RecruitmentRequestStatus.PendingHR,
                demo.ManagerAccount.Id,
                "Can tuyen ky su phan mem cho du an ERP thang nay.",
                DateTime.UtcNow.AddDays(20),
                ct);

            var openJob = await EnsureRecruitmentAsync(
                context,
                org.Product,
                org.AnalystPosition,
                2,
                RecruitmentRequestStatus.Approved,
                demo.HrAccount.Id,
                "Tin tuyen dung dang mo cho vi tri phan tich du lieu san pham.",
                DateTime.UtcNow.AddDays(30),
                ct);

            await context.SaveChangesAsync(ct);

            await EnsureCandidateAsync(context, openJob, "Demo Ung vien Moi", "candidate.new.demo@hicas.vn", "TRK-DEMO-NEW", CandidateStatus.New, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule3ProfileAndContractsAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            await EnsureDependentAsync(context, demo.Employee, "Pham Bao Chau", DependentRelation.Child, ct);
            await EnsureProfileUpdateRequestAsync(context, demo.Employee, RequestStatus.Pending_HR, ct);

            await EnsureContractAsync(
                context,
                demo.Employee,
                "DEMO-HD-EMP01-2026",
                ContractType.Definite,
                ContractLegalDocumentType.FixedTermLaborContract,
                ContractStatus.Active,
                18000000m,
                DateTime.UtcNow.AddMonths(-4).Date,
                DateTime.UtcNow.AddMonths(8).Date,
                ct);

            await EnsureEmploymentHistoryAsync(context, demo.Employee, HistoryType.Onboarding, "Probation", "Official", DateTime.UtcNow.AddMonths(-4), demo.Hr, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule4AttendanceAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            var shift = await EnsureWorkShiftAsync(context, org.Tech, ct);
            var leaveType = await EnsureLeaveTypeAsync(context, ct);
            var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var period = $"{periodStart.Month:D2}-{periodStart.Year}";

            await EnsureLeaveBalanceAsync(context, demo.Employee, leaveType, (short)periodStart.Year, ct);

            for (var day = 1; day <= Math.Min(10, DateTime.UtcNow.Day); day++)
            {
                var workDate = new DateTime(periodStart.Year, periodStart.Month, day);
                await EnsureAttendanceLogAsync(context, demo.Employee, shift, workDate, day % 4 == 0 ? 18 : 3, ct);
                await EnsureDailySummaryAsync(context, demo.Employee, workDate, period, day % 4 == 0, AttendancePayrollApprovalStatus.Draft, ct);
            }

            await EnsureAttendanceSummaryAsync(context, demo.Employee, periodStart.Month, (short)periodStart.Year, AttendancePayrollApprovalStatus.Draft, demo.HrAccount.Id, null, false, ct);
            await EnsureAttendanceSummaryAsync(context, demo.Intern, periodStart.Month, (short)periodStart.Year, AttendancePayrollApprovalStatus.Draft, demo.HrAccount.Id, null, false, ct);

            await EnsureLeaveRequestAsync(context, demo.Employee, leaveType, LeaveRequestStatus.PendingDept, period, ct);
            await EnsureOvertimeRequestAsync(context, demo.Employee, demo.EmployeeAccount.Id, OvertimeRequestStatus.PendingManager, period, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule5PerformanceAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            var period = $"{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Year}";
            var batch = await EnsureKpiBatchAsync(context, org.Tech, demo.HrAccount.Id, period, ct);

            await EnsurePerformanceReviewAsync(context, demo.Employee, org.Tech, batch, demo.HrAccount.Id, demo.ManagerAccount.Id, period, ReviewStatus.PendingEmployeeUpdate, 0m, ct);
            await EnsurePerformanceReviewAsync(context, demo.Intern, org.Tech, batch, demo.HrAccount.Id, demo.ManagerAccount.Id, period, ReviewStatus.PendingEmployeeUpdate, 0m, ct);

            await EnsurePenaltyRecordAsync(context, demo.Employee, null, demo.HrAccount.Id, PenaltyRecordStatus.PendingHRReview, ct);

            var training = await EnsureTrainingAsync(context, demo.Intern, org.Tech, demo.Manager, TrainingStatus.InProgress, ct);
            await EnsureTaskAsync(context, demo.Employee, org.Tech, demo.ManagerAccount.Id, null, "Demo Task: Hoan thanh module bao cao", TaskStatus.InProgress, 35, ct);
            await EnsureTaskAsync(context, demo.Intern, org.Tech, demo.ManagerAccount.Id, training, "Demo Task: Hoc quy trinh onboarding", TaskStatus.InProgress, 45, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule6PayrollAsync(MyDbContext context, DemoUsers demo, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var period = $"{now.Month:D2}-{now.Year}";

            await EnsurePayrollFormulaAsync(context, demo.HrAccount.Id, demo.DirectorAccount.Id, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Employee, "KPI_BONUS", "Thuong KPI muc tieu", SalaryComponentGroup.Bonus, 5000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Employee, "MEAL_ALLOWANCE", "Phu cap an trua", SalaryComponentGroup.Allowance, 730000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Intern, "INTERN_ALLOWANCE", "Tro cap thuc tap", SalaryComponentGroup.Allowance, InternMonthlyAllowance, ct);

            var payroll = await EnsurePayrollAsync(context, demo.Employee, now.Month, now.Year, period, PayrollStatus.Calculated, demo.HrAccount.Id, null, false, ct);
            await EnsurePayrollDetailAsync(context, payroll, "BASE_SALARY_ACTUAL", "Luong co ban theo cong", 18000000m, true, false, ct);
            await EnsurePayrollDetailAsync(context, payroll, "KPI_BONUS", "Thuong KPI", 4370000m, true, false, ct);
            await EnsurePayrollDetailAsync(context, payroll, "EMPLOYEE_INSURANCE", "Nguoi lao dong dong BH", 1890000m, false, true, ct);
            await EnsurePayrollDetailAsync(context, payroll, "PIT", "Thue TNCN", 620000m, false, true, ct);

            var internPayroll = await EnsurePayrollAsync(context, demo.Intern, now.Month, now.Year, period, PayrollStatus.Calculated, demo.HrAccount.Id, null, false, ct);
            ApplyInternPayrollSnapshot(internPayroll, InternMonthlyAllowance);
            await EnsurePayrollDetailAsync(context, internPayroll, "INTERN_ALLOWANCE", "Tro cap thuc tap", InternMonthlyAllowance, true, false, ct, taxable: false);

            await EnsurePayrollAsync(context, demo.Manager, now.Month, now.Year, period, PayrollStatus.Calculated, demo.HrAccount.Id, null, false, ct);
            await EnsureProjectBonusAsync(context, demo, now.Month, now.Year, period, ct);
            await EnsureExternalTimesheetAsync(context, demo, now.Month, now.Year, period, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureModule7PersonnelChangesAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            var period = $"{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Year}";
            var review = await context.PerformanceReviews
                .FirstOrDefaultAsync(r => r.EmployeeId == demo.Employee.Id && r.Period == period, ct);

            var penalty = await context.PenaltyRecords
                .FirstOrDefaultAsync(p => p.EmployeeId == demo.Employee.Id && p.RuleCode == "DEMO_POLICY_VIOLATION", ct);

            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.Promotion, PersonnelChangeStatus.PendingHRReview, review?.Id, null, ct);
            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.SeniorAppointment, PersonnelChangeStatus.PendingEmployeeConsent, review?.Id, null, ct);
            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.InternalTransfer, PersonnelChangeStatus.PendingHRReview, null, null, ct);
            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.VoluntaryTermination, PersonnelChangeStatus.PendingHRReview, null, null, ct);
            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.Dismissal, PersonnelChangeStatus.PendingHRReview, null, penalty?.Id, ct);
            await EnsurePersonnelChangeAsync(context, demo, org, PersonnelChangeType.ConvertToOfficial, PersonnelChangeStatus.PendingHRReview, review?.Id, null, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureReferenceCoverageAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await EnsureCompanyCalendarAsync(context, demo.AdminAccount.Id, (short)now.Year, ct);

            await EnsureContractAsync(context, demo.Director, "DEMO-HD-DIR01-2026", ContractType.Indefinite, ContractLegalDocumentType.IndefiniteTermLaborContract, ContractStatus.Active, 55000000m, now.AddMonths(-10).Date, null, ct);
            await EnsureContractAsync(context, demo.Hr, "DEMO-HD-HR01-2026", ContractType.Definite, ContractLegalDocumentType.FixedTermLaborContract, ContractStatus.Active, 22000000m, now.AddMonths(-8).Date, now.AddMonths(16).Date, ct);
            await EnsureContractAsync(context, demo.Manager, "DEMO-HD-MAN01-ACTIVE-2026", ContractType.Indefinite, ContractLegalDocumentType.IndefiniteTermLaborContract, ContractStatus.Active, 32000000m, now.AddMonths(-9).Date, null, ct);
            await EnsureContractAsync(context, demo.Collaborator, "DEMO-HD-COL01-2026", ContractType.PartTime, ContractLegalDocumentType.FixedTermLaborContract, ContractStatus.Active, 0m, now.AddMonths(-3).Date, now.AddMonths(3).Date, ct);

            var directorShift = await EnsureWorkShiftAsync(context, org.Bod, ct);
            var hrShift = await EnsureWorkShiftAsync(context, org.Hr, ct);
            var techShift = await EnsureWorkShiftAsync(context, org.Tech, ct);

            await EnsureAttendanceLogAsync(context, demo.Director, directorShift, now.Date.AddDays(-2), 0, ct);
            await EnsureAttendanceLogAsync(context, demo.Hr, hrShift, now.Date.AddDays(-2), 4, ct);
            await EnsureAttendanceLogAsync(context, demo.Manager, techShift, now.Date.AddDays(-2), 2, ct);
            await RemoveCollaboratorInternalTimekeepingAsync(context, demo.Collaborator, ct);

            await EnsureEmployeeSalaryComponentAsync(context, demo.Director, "PROJECT_BONUS", "Thuong du an", SalaryComponentGroup.Bonus, 8000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Hr, "MEAL_ALLOWANCE", "Phu cap an trua", SalaryComponentGroup.Allowance, 730000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Manager, "KPI_BONUS", "Thuong KPI muc tieu", SalaryComponentGroup.Bonus, 7000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Collaborator, "EXTERNAL_TIMESHEET_PAY", "Thu lao cong tac vien", SalaryComponentGroup.Bonus, 5400000m, ct);
            await context.SaveChangesAsync(ct);
        }

        private static async Task AddSeedMarkerAsync(MyDbContext context, string key, string description, CancellationToken ct)
        {
            if (await context.Configurations.AnyAsync(c => c.ConfigGroup == MarkerGroup && c.ParamKey == key, ct))
                return;

            context.Configurations.Add(new AppConfig
            {
                ConfigGroup = MarkerGroup,
                ParamKey = key,
                ParamValue = DateTime.UtcNow.ToString("O"),
                Description = description,
                IsActive = true
            });
        }

        private static async Task EnsureJulyPayrollSandboxDataAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            CancellationToken ct)
        {
            const byte month = 7;
            const short year = 2026;
            const string payrollPeriod = "07-2026";
            const string kpiPeriod = "07/2026";
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1);

            var internalEmployees = new[] { demo.Director, demo.Hr, demo.Manager, demo.Employee, demo.Intern };
            var allDemoEmployeeIds = new[]
            {
                demo.Director.Id,
                demo.Hr.Id,
                demo.Manager.Id,
                demo.Employee.Id,
                demo.Collaborator.Id,
                demo.Intern.Id
            };

            await ResetJulyPayrollSandboxDataAsync(context, allDemoEmployeeIds, month, year, payrollPeriod, kpiPeriod, ct);
            await EnsureCompanyCalendarAsync(context, demo.AdminAccount.Id, year, ct);

            await EnsureContractAsync(context, demo.Employee, "DEMO-HD-EMP01-2026", ContractType.Definite, ContractLegalDocumentType.FixedTermLaborContract, ContractStatus.Active, 18000000m, new DateTime(2026, 1, 1), new DateTime(2027, 1, 31), ct);
            await EnsureContractAsync(context, demo.Director, "DEMO-HD-DIR01-2026", ContractType.Indefinite, ContractLegalDocumentType.IndefiniteTermLaborContract, ContractStatus.Active, 55000000m, new DateTime(2025, 8, 1), null, ct);
            await EnsureContractAsync(context, demo.Hr, "DEMO-HD-HR01-2026", ContractType.Definite, ContractLegalDocumentType.FixedTermLaborContract, ContractStatus.Active, 22000000m, new DateTime(2025, 10, 1), new DateTime(2027, 10, 1), ct);
            await EnsureContractAsync(context, demo.Manager, "DEMO-HD-MAN01-ACTIVE-2026", ContractType.Indefinite, ContractLegalDocumentType.IndefiniteTermLaborContract, ContractStatus.Active, 32000000m, new DateTime(2025, 9, 1), null, ct);
            await EnsureContractAsync(context, demo.Collaborator, "DEMO-HD-COL01-2026", ContractType.PartTime, ContractLegalDocumentType.FixedTermLaborContract, ContractStatus.Active, 0m, new DateTime(2026, 4, 1), new DateTime(2026, 12, 31), ct);
            await EnsureContractAsync(context, demo.Intern, "DEMO-HD-INT01-ACTIVE-2026", ContractType.Probation, ContractLegalDocumentType.ProbationContract, ContractStatus.Active, 0m, new DateTime(2026, 6, 1), new DateTime(2026, 8, 31), ct);

            await EnsureEmployeeSalaryComponentAsync(context, demo.Employee, "KPI_BONUS", "Thuong KPI muc tieu", SalaryComponentGroup.Bonus, 5000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Employee, "MEAL_ALLOWANCE", "Phu cap an trua", SalaryComponentGroup.Allowance, 730000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Director, "PROJECT_BONUS", "Thuong du an", SalaryComponentGroup.Bonus, 8000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Manager, "KPI_BONUS", "Thuong KPI muc tieu", SalaryComponentGroup.Bonus, 7000000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Hr, "MEAL_ALLOWANCE", "Phu cap an trua", SalaryComponentGroup.Allowance, 730000m, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Intern, "INTERN_ALLOWANCE", "Tro cap thuc tap", SalaryComponentGroup.Allowance, InternMonthlyAllowance, ct);
            await EnsureEmployeeSalaryComponentAsync(context, demo.Collaborator, "EXTERNAL_TIMESHEET_PAY", "Thu lao cong tac vien", SalaryComponentGroup.Bonus, 5400000m, ct);

            var bodShift = await EnsureWorkShiftAsync(context, org.Bod, ct);
            var hrShift = await EnsureWorkShiftAsync(context, org.Hr, ct);
            var techShift = await EnsureWorkShiftAsync(context, org.Tech, ct);

            await SeedJulyAttendanceAsync(context, demo.Director, bodShift, periodStart, periodEnd, 0, 0, payrollPeriod, ct);
            await SeedJulyAttendanceAsync(context, demo.Hr, hrShift, periodStart, periodEnd, 1, 0, payrollPeriod, ct);
            await SeedJulyAttendanceAsync(context, demo.Manager, techShift, periodStart, periodEnd, 2, 60, payrollPeriod, ct);
            await SeedJulyAttendanceAsync(context, demo.Employee, techShift, periodStart, periodEnd, 3, 120, payrollPeriod, ct);
            await SeedJulyAttendanceAsync(context, demo.Intern, techShift, periodStart, periodEnd, 4, 0, payrollPeriod, ct, halfDayEveryFriday: true);

            var leaveType = await EnsureLeaveTypeAsync(context, ct);
            foreach (var employee in internalEmployees)
                await EnsureLeaveBalanceAsync(context, employee, leaveType, year, ct);

            await EnsureJulyKpiAsync(context, demo, org, kpiPeriod, ct);
            await EnsureJulyProjectBonusDraftAsync(context, demo, month, year, payrollPeriod, ct);
            await EnsureJulyExternalTimesheetDraftAsync(context, demo, month, year, payrollPeriod, ct);

            await context.SaveChangesAsync(ct);
        }

        private static async Task ResetJulyPayrollSandboxDataAsync(
            MyDbContext context,
            IReadOnlyCollection<int> demoEmployeeIds,
            byte month,
            short year,
            string payrollPeriod,
            string kpiPeriod,
            CancellationToken ct)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1);

            var payrollIds = await context.Payrolls
                .Where(p => p.EmployeeId.HasValue && demoEmployeeIds.Contains(p.EmployeeId.Value) && p.Month == month && p.Year == year)
                .Select(p => p.Id)
                .ToListAsync(ct);
            await RemoveApprovalRequestsAsync(context, "PAYROLL_RUN_APPROVAL", payrollIds, ct);
            if (payrollIds.Count > 0)
            {
                context.PayrollDetails.RemoveRange(await context.PayrollDetails.Where(d => payrollIds.Contains(d.PayrollId)).ToListAsync(ct));
                context.PayrollContractSegments.RemoveRange(await context.PayrollContractSegments.Where(s => s.PayrollId.HasValue && payrollIds.Contains(s.PayrollId.Value)).ToListAsync(ct));
                context.Payrolls.RemoveRange(await context.Payrolls.Where(p => payrollIds.Contains(p.Id)).ToListAsync(ct));
            }

            context.AttendanceSummaries.RemoveRange(await context.AttendanceSummaries
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) && a.Month == month && a.Year == year)
                .ToListAsync(ct));
            context.AttendanceDailySummaries.RemoveRange(await context.AttendanceDailySummaries
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) && a.WorkDate >= periodStart && a.WorkDate < periodEnd)
                .ToListAsync(ct));
            context.AttendanceLogs.RemoveRange(await context.AttendanceLogs
                .Where(a => a.EmployeeId.HasValue && demoEmployeeIds.Contains(a.EmployeeId.Value) && a.WorkDate >= periodStart && a.WorkDate < periodEnd)
                .ToListAsync(ct));

            var projectBonusBatches = await context.ProjectBonusImportBatches
                .Include(b => b.Lines)
                .Where(b => b.PayrollPeriod == payrollPeriod && b.FileName == "demo-project-bonus-july-2026.csv")
                .ToListAsync(ct);
            await RemoveApprovalRequestsAsync(context, "PROJECT_BONUS_IMPORT", projectBonusBatches.Select(b => b.Id).ToList(), ct);
            if (projectBonusBatches.Count > 0)
            {
                context.ProjectBonusImportLines.RemoveRange(projectBonusBatches.SelectMany(b => b.Lines));
                context.ProjectBonusImportBatches.RemoveRange(projectBonusBatches);
            }

            var externalTimesheetImports = await context.ExternalTimesheetImports
                .Include(i => i.Lines)
                .Where(i => i.PayrollPeriod == payrollPeriod && i.FileName == "demo-external-timesheet-july-2026.csv")
                .ToListAsync(ct);
            await RemoveApprovalRequestsAsync(context, "EXTERNAL_TIMESHEET_IMPORT", externalTimesheetImports.Select(i => i.Id).ToList(), ct);
            if (externalTimesheetImports.Count > 0)
            {
                context.ExternalTimesheetLines.RemoveRange(externalTimesheetImports.SelectMany(i => i.Lines));
                context.ExternalTimesheetImports.RemoveRange(externalTimesheetImports);
            }

            context.PayrollAdjustments.RemoveRange(await context.PayrollAdjustments
                .Where(a => demoEmployeeIds.Contains(a.EmployeeId) &&
                            a.RecognizedMonth == month &&
                            a.RecognizedYear == year &&
                            a.Reason != null &&
                            a.Reason.Contains("Demo July"))
                .ToListAsync(ct));

            var kpiReviewIds = await context.PerformanceReviews
                .Where(r => demoEmployeeIds.Contains(r.EmployeeId) && r.Period == kpiPeriod)
                .Select(r => r.Id)
                .ToListAsync(ct);
            await RemoveApprovalRequestsAsync(context, "PERFORMANCE_APPROVAL", kpiReviewIds, ct);
            if (kpiReviewIds.Count > 0)
            {
                context.PerformanceDetails.RemoveRange(await context.PerformanceDetails.Where(d => kpiReviewIds.Contains(d.ReviewId)).ToListAsync(ct));
                context.PerformanceReviews.RemoveRange(await context.PerformanceReviews.Where(r => kpiReviewIds.Contains(r.Id)).ToListAsync(ct));
            }

            context.KpiImportBatches.RemoveRange(await context.KpiImportBatches
                .Where(b => b.Period == kpiPeriod && b.FileName == "demo-kpi-july-2026.xlsx")
                .ToListAsync(ct));

            await context.SaveChangesAsync(ct);
        }

        private static async Task RemoveApprovalRequestsAsync(MyDbContext context, string moduleCode, IReadOnlyCollection<int> referenceIds, CancellationToken ct)
        {
            if (referenceIds.Count == 0) return;

            var requests = await context.ApprovalRequests
                .Include(r => r.Steps)
                .Where(r => r.ModuleCode == moduleCode && referenceIds.Contains(r.ReferenceId))
                .ToListAsync(ct);
            if (requests.Count == 0) return;

            context.ApprovalSteps.RemoveRange(requests.SelectMany(r => r.Steps));
            context.ApprovalRequests.RemoveRange(requests);
        }

        private static async Task SeedJulyAttendanceAsync(
            MyDbContext context,
            Employee employee,
            WorkShift shift,
            DateTime periodStart,
            DateTime periodEnd,
            int latePatternOffset,
            int seededOvertimeMinutes,
            string payrollPeriod,
            CancellationToken ct,
            bool halfDayEveryFriday = false)
        {
            decimal workDays = 0;
            var workedMinutes = 0;
            var lateMinutes = 0;
            var earlyLeaveMinutes = 0;
            var overtimeMinutes = 0;

            for (var date = periodStart.Date; date < periodEnd.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                var isHalfDay = halfDayEveryFriday && date.DayOfWeek == DayOfWeek.Friday;
                var late = (date.Day + latePatternOffset) % 9 == 0 ? 18 : ((date.Day + latePatternOffset) % 7 == 0 ? 6 : 0);
                var early = isHalfDay ? 240 : 0;
                var dayOt = seededOvertimeMinutes > 0 && date.Day == 10 ? seededOvertimeMinutes : 0;
                var checkIn = date.AddHours(8).AddMinutes(late);
                var checkOut = isHalfDay ? date.AddHours(12) : date.AddHours(17).AddMinutes(30 + dayOt);
                var dayWorkedMinutes = isHalfDay ? 240 : 480;
                var workdayValue = isHalfDay ? 0.5m : 1m;

                context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    ShiftId = shift.Id,
                    WorkDate = date,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    IpAddress = "192.168.1.100",
                    GpsLat = 21.004118m,
                    GpsLong = 105.843381m,
                    Status = late > shift.LateThresholdMins ? AttendanceStatus.Late : AttendanceStatus.Valid
                });

                context.AttendanceDailySummaries.Add(new AttendanceDailySummary
                {
                    EmployeeId = employee.Id,
                    WorkDate = date,
                    FirstCheckIn = checkIn,
                    LastCheckOut = checkOut,
                    WorkingMinutes = dayWorkedMinutes,
                    LateMinutes = late,
                    EarlyLeaveMinutes = early,
                    OvertimeMinutes = dayOt,
                    WorkdayValue = workdayValue,
                    AttendanceStatus = isHalfDay ? AttendanceDailyStatus.HalfDay : AttendanceDailyStatus.Present,
                    ApprovalStatus = AttendancePayrollApprovalStatus.Draft,
                    PayrollPeriod = payrollPeriod.Replace("-", "/"),
                    GeneratedAt = DateTime.UtcNow
                });

                workDays += workdayValue;
                workedMinutes += dayWorkedMinutes;
                lateMinutes += late;
                earlyLeaveMinutes += early;
                overtimeMinutes += dayOt;
            }

            context.AttendanceSummaries.Add(new AttendanceSummary
            {
                EmployeeId = employee.Id,
                Month = (byte)periodStart.Month,
                Year = (short)periodStart.Year,
                WorkDays = workDays,
                WorkedMinutes = workedMinutes,
                PayableWorkHours = Math.Round(workedMinutes / 60m, 2, MidpointRounding.AwayFromZero),
                LateMinutes = lateMinutes,
                EarlyLeaveMinutes = earlyLeaveMinutes,
                ActualOtMinutes = overtimeMinutes,
                ApprovalStatus = AttendancePayrollApprovalStatus.Draft,
                PeriodNote = "Demo July payroll sandbox: bảng công nháp để HR tự điều chỉnh, gửi chốt và khóa kỳ.",
                IsPayrollLocked = false,
                GeneratedAt = DateTime.UtcNow
            });

            await Task.CompletedTask;
        }

        private static async Task EnsureJulyKpiAsync(MyDbContext context, DemoUsers demo, DemoOrg org, string period, CancellationToken ct)
        {
            var techBatch = await EnsureJulyKpiBatchAsync(context, org.Tech, demo.HrAccount.Id, period, ct);
            var hrBatch = await EnsureJulyKpiBatchAsync(context, org.Hr, demo.HrAccount.Id, period, ct);
            var bodBatch = await EnsureJulyKpiBatchAsync(context, org.Bod, demo.HrAccount.Id, period, ct);

            await EnsurePerformanceReviewAsync(context, demo.Employee, org.Tech, techBatch, demo.HrAccount.Id, demo.ManagerAccount.Id, period, ReviewStatus.Evaluated, 88m, ct);
            await EnsurePerformanceReviewAsync(context, demo.Manager, org.Tech, techBatch, demo.HrAccount.Id, demo.DirectorAccount.Id, period, ReviewStatus.Evaluated, 92m, ct);
            await EnsurePerformanceReviewAsync(context, demo.Intern, org.Tech, techBatch, demo.HrAccount.Id, demo.ManagerAccount.Id, period, ReviewStatus.Evaluated, 78m, ct);
            await EnsurePerformanceReviewAsync(context, demo.Hr, org.Hr, hrBatch, demo.HrAccount.Id, demo.DirectorAccount.Id, period, ReviewStatus.Evaluated, 85m, ct);
            await EnsurePerformanceReviewAsync(context, demo.Director, org.Bod, bodBatch, demo.HrAccount.Id, demo.AdminAccount.Id, period, ReviewStatus.Evaluated, 90m, ct);
        }

        private static async Task<KpiImportBatch> EnsureJulyKpiBatchAsync(MyDbContext context, Department dept, int importedBy, string period, CancellationToken ct)
        {
            var entity = await context.KpiImportBatches.FirstOrDefaultAsync(b => b.Period == period && b.DeptId == dept.Id && b.FileName == "demo-kpi-july-2026.xlsx", ct);
            if (entity != null) return entity;

            entity = new KpiImportBatch
            {
                Period = period,
                DeptId = dept.Id,
                ImportedByAccountId = importedBy,
                FileName = "demo-kpi-july-2026.xlsx",
                TotalRows = 15,
                SuccessRows = 15,
                ErrorRows = 0,
                Status = ImportBatchStatus.Completed
            };
            context.KpiImportBatches.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsureJulyProjectBonusDraftAsync(MyDbContext context, DemoUsers demo, byte month, short year, string period, CancellationToken ct)
        {
            var batch = new ProjectBonusImportBatch
            {
                PeriodMonth = month,
                PeriodYear = year,
                PayrollPeriod = period,
                FileName = "demo-project-bonus-july-2026.csv",
                UploadedByAccountId = demo.HrAccount.Id,
                Status = ProjectBonusImportStatus.Draft,
                TotalRows = 4,
                ValidRows = 4,
                ErrorRows = 0,
                TotalAmount = 18500000m,
                Note = "Demo July: batch thưởng dự án ở bản nháp để HR tự gửi duyệt.",
                CreatedAt = DateTime.UtcNow
            };
            context.ProjectBonusImportBatches.Add(batch);
            await context.SaveChangesAsync(ct);

            context.ProjectBonusImportLines.AddRange(
                ProjectBonusLine(batch.Id, 1, demo.Employee, "HICAS-ERP", "Go-live ERP tháng 7", 3500000m),
                ProjectBonusLine(batch.Id, 2, demo.Manager, "HICAS-BIM", "BIM Platform tháng 7", 3000000m),
                ProjectBonusLine(batch.Id, 3, demo.Director, "HICAS-OPS", "Điều phối chuyển đổi số tháng 7", 8000000m),
                ProjectBonusLine(batch.Id, 4, demo.Hr, "HICAS-HRM", "Vận hành HRM nội bộ tháng 7", 4000000m));
        }

        private static async Task EnsureJulyExternalTimesheetDraftAsync(MyDbContext context, DemoUsers demo, byte month, short year, string period, CancellationToken ct)
        {
            var import = new ExternalTimesheetImport
            {
                SourceSystem = "Demo July Portal",
                ImportMonth = month,
                ImportYear = year,
                PayrollPeriod = period,
                FileName = "demo-external-timesheet-july-2026.csv",
                ImportedByAccountId = demo.HrAccount.Id,
                ImportedAt = DateTime.UtcNow,
                Status = ExternalTimesheetImportStatus.Draft,
                TotalRows = 3,
                ValidRows = 3,
                ErrorRows = 0,
                TotalHours = 48,
                TotalAmount = 7200000m,
                Note = "Demo July: giờ công cộng tác viên ở bản nháp để HR tự gửi duyệt."
            };
            context.ExternalTimesheetImports.Add(import);
            await context.SaveChangesAsync(ct);

            context.ExternalTimesheetLines.AddRange(
                ExternalLine(import.Id, 1, demo.Collaborator, new DateTime(year, month, 3), 16, 150000m),
                ExternalLine(import.Id, 2, demo.Collaborator, new DateTime(year, month, 11), 18, 150000m),
                ExternalLine(import.Id, 3, demo.Collaborator, new DateTime(year, month, 21), 14, 150000m));
        }

        private static async Task EnsureCompanyCalendarAsync(MyDbContext context, int adminAccountId, short year, CancellationToken ct)
        {
            var versionCode = $"DEMO_HICAS_CALENDAR_{year}";
            var calendar = await context.CompanyCalendars
                .Include(c => c.Days)
                .FirstOrDefaultAsync(c => c.Year == year && c.VersionCode == versionCode, ct);

            if (calendar == null)
            {
                calendar = new CompanyCalendar
                {
                    Year = year,
                    VersionCode = versionCode,
                    EffectiveFrom = new DateTime(year, 1, 1),
                    EffectiveTo = new DateTime(year, 12, 31),
                    Status = PolicyVersionStatus.Active,
                    SourceRef = "DEMO_SCREENSHOT_SEED",
                    CreatedByAccountId = adminAccountId,
                    ActivatedAt = new DateTime(year, 1, 1),
                    Note = "Demo lich nghi cong ty dung chung cho cham cong, OT va payroll."
                };
                context.CompanyCalendars.Add(calendar);
                await context.SaveChangesAsync(ct);
            }

            calendar.Status = PolicyVersionStatus.Active;
            calendar.ActivatedAt ??= new DateTime(year, 1, 1);

            await EnsureCalendarDayAsync(context, calendar.Id, new DateTime(year, 1, 1), CompanyCalendarDayType.PublicHoliday, "Tet duong lich", true, true, false, ct);
            await EnsureCalendarDayAsync(context, calendar.Id, new DateTime(year, 4, 30), CompanyCalendarDayType.PublicHoliday, "Ngay Giai phong mien Nam", true, true, false, ct);
            await EnsureCalendarDayAsync(context, calendar.Id, new DateTime(year, 5, 1), CompanyCalendarDayType.PublicHoliday, "Quoc te Lao dong", true, true, false, ct);
            await EnsureCalendarDayAsync(context, calendar.Id, new DateTime(year, 6, 20), CompanyCalendarDayType.CompanyHoliday, "HICAS Company Day", true, true, false, ct);
            await EnsureCalendarDayAsync(context, calendar.Id, new DateTime(year, 6, 27), CompanyCalendarDayType.CompensatoryWorkingDay, "Ngay lam bu demo", true, false, true, ct);
        }

        private static async Task EnsureCalendarDayAsync(
            MyDbContext context,
            int calendarId,
            DateTime date,
            CompanyCalendarDayType type,
            string name,
            bool paid,
            bool overtimeHoliday,
            bool workingOverride,
            CancellationToken ct)
        {
            var entity = await context.CompanyCalendarDays
                .FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date == date.Date, ct);

            if (entity == null)
            {
                entity = new CompanyCalendarDay
                {
                    CalendarId = calendarId,
                    Date = date.Date,
                    CreatedAt = DateTime.UtcNow
                };
                context.CompanyCalendarDays.Add(entity);
            }

            entity.DayType = type;
            entity.Name = name;
            entity.IsPaid = paid;
            entity.IsOvertimeHoliday = overtimeHoliday;
            entity.IsWorkingDayOverride = workingOverride;
            entity.Description = "Demo ngay lich cong ty phuc vu anh chup minh chung.";
        }

        private static void ApplyInternPayrollSnapshot(Payroll payroll, decimal allowance)
        {
            payroll.BaseSalary = 0m;
            payroll.BaseSalaryActual = 0m;
            payroll.GrossSalary = allowance;
            payroll.GrossIncome = allowance;
            payroll.TotalAllowance = allowance;
            payroll.TotalBonus = 0m;
            payroll.InsuranceSalary = 0m;
            payroll.InsuranceDeduction = 0m;
            payroll.EmployeeInsuranceAmount = 0m;
            payroll.EmployerContributionAmount = 0m;
            payroll.TaxDeductionPersonal = 0m;
            payroll.TaxDeductionFamily = 0m;
            payroll.TaxableGrossIncome = 0m;
            payroll.TaxableIncome = 0m;
            payroll.PitAmount = 0m;
            payroll.OtherDeductions = 0m;
            payroll.NetSalary = allowance;
            payroll.TotalCompanyCost = allowance;
            payroll.ActualWorkDays = 20;
            payroll.ActualWorkHours = 160;
            payroll.ActualOtMinutes = 0;
            payroll.ReviewNote = "Demo tro cap thuc tap theo hop dong, khong tinh OT, BH hay thue.";
        }

        private static async Task<Department> EnsureDepartmentAsync(MyDbContext context, string code, string name, CancellationToken ct)
        {
            var entity = await context.Departments.FirstOrDefaultAsync(d => d.DeptCode == code, ct);
            if (entity != null) return entity;

            entity = new Department { DeptCode = code, DeptName = name, Status = DeptStatus.Active };
            context.Departments.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task<Position> EnsurePositionAsync(MyDbContext context, string title, int jobLevel, CancellationToken ct)
        {
            var entity = await context.Positions.FirstOrDefaultAsync(p => p.Title == title, ct);
            if (entity != null) return entity;

            entity = new Position { Title = title, JobLevel = jobLevel, IsActive = true };
            context.Positions.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task<JobLevel> EnsureJobLevelAsync(MyDbContext context, string code, string name, int rank, bool isManagement, CancellationToken ct)
        {
            var entity = await context.JobLevels.FirstOrDefaultAsync(j => j.Code == code, ct);
            if (entity != null) return entity;

            entity = new JobLevel { Code = code, Name = name, RankOrder = rank, IsManagementLevel = isManagement, IsActive = true };
            context.JobLevels.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task<Account> EnsureAccountAsync(MyDbContext context, string email, string fullName, Role role, CancellationToken ct)
        {
            var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
            if (entity == null)
            {
                entity = new Account
                {
                    Email = email,
                    FullName = fullName,
                    RoleId = role.Id,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
                    Status = AccountStatus.Active
                };
                context.Accounts.Add(entity);
            }
            else
            {
                entity.FullName = fullName;
                entity.RoleId = role.Id;
                entity.Status = AccountStatus.Active;
                entity.PasswordHash ??= BCrypt.Net.BCrypt.HashPassword(DemoPassword);
            }

            return entity;
        }

        private static async Task<Employee> EnsureEmployeeAsync(
            MyDbContext context,
            Account account,
            string code,
            string fullName,
            Department department,
            Position position,
            JobLevel jobLevel,
            Employee? manager,
            EmployeeType type,
            EmployeeStatus status,
            CancellationToken ct)
        {
            var entity = await context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == code, ct);
            var demoDigits = DemoDigits(code, 8);
            if (entity == null)
            {
                entity = new Employee
                {
                    AccountId = account.Id,
                    EmployeeCode = code,
                    FullName = fullName,
                    PersonalEmail = account.Email,
                    PhoneNumber = "090" + demoDigits[..7],
                    CurrentAddress = "Tang 5, HICAS Tower, Ha Noi",
                    PermanentAddress = "Ha Noi",
                    IdentityNumber = "DEMOID" + demoDigits,
                    TaxCode = "DEMO-TAX-" + code.Replace("DEMO-", ""),
                    SocialInsCode = "DEMO-SI-" + code.Replace("DEMO-", ""),
                    BankAccount = "9704" + demoDigits,
                    BankName = "Vietcombank",
                    Gender = code.Contains("HR") || code.Contains("COL") ? Gender.Female : Gender.Male,
                    BirthDate = DateTime.UtcNow.AddYears(-28),
                    Nationality = "Viet Nam",
                    Ethnicity = "Kinh",
                    DeptId = department.Id,
                    PositionId = position.Id,
                    JobLevelId = jobLevel.Id,
                    ManagerId = manager?.Id,
                    Type = type,
                    Status = status,
                    JoinedDate = DateTime.UtcNow.AddMonths(-8),
                    ResidenceStatus = ResidenceStatus.Resident,
                    TaxCodeStatus = TaxCodeStatus.Registered,
                    EmergencyContactName = "Demo Emergency",
                    EmergencyPhone = "0988000000",
                    EmergencyRelation = "Family"
                };
                context.Employees.Add(entity);
                await context.SaveChangesAsync(ct);
            }
            else
            {
                entity.AccountId = account.Id;
                entity.FullName = fullName;
                entity.PersonalEmail = account.Email;
                entity.DeptId = department.Id;
                entity.PositionId = position.Id;
                entity.JobLevelId = jobLevel.Id;
                entity.ManagerId = manager?.Id;
                entity.Type = type;
                entity.Status = status;
                entity.Nationality ??= "Viet Nam";
                entity.Ethnicity ??= "Kinh";
                entity.PhoneNumber ??= "090" + demoDigits[..7];
                entity.IdentityNumber ??= "DEMOID" + demoDigits;
                entity.BankAccount ??= "9704" + demoDigits;
            }

            return entity;
        }

        private static string DemoDigits(string seed, int length)
        {
            var hash = Math.Abs((long)seed.GetHashCode());
            return (hash % 100000000).ToString().PadLeft(length, '0');
        }

        private static AuditLog Audit(int accountId, string action, string table, string? oldValues, string? newValues, string note)
        {
            return new AuditLog
            {
                AccountId = accountId,
                ActionType = action,
                TableName = table,
                OldValues = oldValues,
                NewValues = newValues,
                AffectedColumns = note,
                Timestamp = DateTime.UtcNow
            };
        }

        private static async Task<RecruitmentRequest> EnsureRecruitmentAsync(
            MyDbContext context,
            Department department,
            Position position,
            int quantity,
            RecruitmentRequestStatus status,
            int createdBy,
            string description,
            DateTime deadline,
            CancellationToken ct)
        {
            var entity = await context.RecruitmentRequests
                .FirstOrDefaultAsync(r => r.Description == description && r.CreatedById == createdBy, ct);
            if (entity == null)
            {
                entity = new RecruitmentRequest
                {
                    DeptId = department.Id,
                    PositionId = position.Id,
                    Quantity = quantity,
                    Description = description,
                    Deadline = deadline,
                    Status = status,
                    CreatedById = createdBy,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                context.RecruitmentRequests.Add(entity);
            }
            else
            {
                entity.Status = status;
                entity.Deadline = deadline;
                entity.Quantity = quantity;
            }

            return entity;
        }

        private static async Task<Candidate> EnsureCandidateAsync(MyDbContext context, RecruitmentRequest request, string name, string email, string trackingCode, CandidateStatus status, CancellationToken ct)
        {
            var entity = await context.Candidates.FirstOrDefaultAsync(c => c.TrackingCode == trackingCode, ct);
            if (entity == null)
            {
                entity = new Candidate
                {
                    RecruitmentRequestId = request.Id,
                    FullName = name,
                    Email = email,
                    TrackingCode = trackingCode,
                    CvFilePath = "/uploads/cvs/demo-cv.pdf",
                    Status = status,
                    AppliedDate = DateTime.UtcNow.AddDays(-2)
                };
                context.Candidates.Add(entity);
            }
            else
            {
                entity.RecruitmentRequestId = request.Id;
                entity.Status = status;
            }

            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsureDependentAsync(MyDbContext context, Employee employee, string name, DependentRelation relation, CancellationToken ct)
        {
            if (await context.Dependents.AnyAsync(d => d.EmployeeId == employee.Id && d.FullName == name, ct)) return;

            context.Dependents.Add(new Dependent
            {
                EmployeeId = employee.Id,
                FullName = name,
                Relationship = relation,
                BirthDate = DateTime.UtcNow.AddYears(-6),
                IdNumber = "DEMO-DEP-001",
                TaxDependentCode = "DEMO-DEP-TAX-001",
                ValidFrom = DateTime.UtcNow.AddMonths(-3),
                IsActive = true,
                EvidenceUrl = "/uploads/dependent-evidences/demo-dependent.pdf",
                Note = "Demo dependent for tax deduction screenshot."
            });
        }

        private static async Task EnsureProfileUpdateRequestAsync(MyDbContext context, Employee employee, RequestStatus status, CancellationToken ct)
        {
            if (await context.ProfileUpdateRequests.AnyAsync(p => p.EmployeeId == employee.Id && p.Status == status, ct)) return;

            context.ProfileUpdateRequests.Add(new ProfileUpdateRequest
            {
                EmployeeId = employee.Id,
                Status = status,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                DeadlineSLA = DateTime.UtcNow.AddHours(66),
                RequestedDataJson = JsonSerializer.Serialize(new
                {
                    PhoneNumber = "0912345678",
                    CurrentAddress = "Can ho demo, Ha Noi",
                    BankName = "Techcombank"
                })
            });
        }

        private static async Task<Contract> EnsureContractAsync(
            MyDbContext context,
            Employee employee,
            string number,
            ContractType type,
            ContractLegalDocumentType legalType,
            ContractStatus status,
            decimal salary,
            DateTime startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var insuranceSalary = employee.Type == EmployeeType.Intern ? 0m : salary;
            var entity = await context.Contracts
                .Include(c => c.LegalSnapshots)
                .FirstOrDefaultAsync(c => c.ContractNumber == number, ct);
            if (entity == null)
            {
                entity = new Contract
                {
                    EmployeeId = employee.Id,
                    ContractNumber = number,
                    ContractType = type,
                    LegalDocumentType = legalType,
                    BasicSalary = salary,
                    InsuranceSalary = insuranceSalary,
                    SalaryPercentage = 100,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = status,
                    LegalDocumentNumber = number,
                    DocumentTemplateCode = legalType switch
                    {
                        ContractLegalDocumentType.ProbationContract => "LABOR_CONTRACT_PROBATION",
                        ContractLegalDocumentType.IndefiniteTermLaborContract => "LABOR_CONTRACT_INDEFINITE",
                        _ => "LABOR_CONTRACT_FIXED_TERM"
                    },
                    IssuedAt = status == ContractStatus.Active ? DateTime.UtcNow.AddMonths(-1) : null
                };
                context.Contracts.Add(entity);
                await context.SaveChangesAsync(ct);
            }

            entity.Status = status;
            entity.BasicSalary = salary;
            entity.InsuranceSalary = insuranceSalary;

            if (!await context.ContractLegalSnapshots.AnyAsync(s => s.ContractId == entity.Id && s.Version == entity.Version, ct))
            {
                context.ContractLegalSnapshots.Add(new ContractLegalSnapshot
                {
                    ContractId = entity.Id,
                    Version = entity.Version,
                    LegalDocumentType = legalType,
                    LegalDocumentNumber = number,
                    DocumentTemplateCode = entity.DocumentTemplateCode,
                    EmployerLegalName = "Cong ty Co phan HICAS",
                    EmployerTaxCode = "0109999999",
                    EmployerAddress = "Tang 5, HICAS Tower, Ha Noi",
                    EmployerRepresentativeName = "Nguyen Minh Quan",
                    EmployerRepresentativeTitle = "Giam doc",
                    SigningLocation = "Ha Noi",
                    EmployeeFullNameSnapshot = employee.FullName,
                    EmployeeBirthDateSnapshot = employee.BirthDate,
                    EmployeeGenderSnapshot = employee.Gender,
                    EmployeeIdentityNumberSnapshot = employee.IdentityNumber,
                    EmployeeResidenceAddressSnapshot = employee.CurrentAddress,
                    EmployeeDepartmentSnapshot = employee.Department?.DeptName,
                    EmployeePositionSnapshot = employee.Position?.Title,
                    EmployeeJobLevelSnapshot = employee.JobLevel?.Name,
                    JobTitle = employee.Position?.Title,
                    JobDescription = "Thuc hien cong viec theo mo ta vi tri va phan cong cua quan ly truc tiep.",
                    WorkLocation = "Ha Noi",
                    WorkingMode = "Toan thoi gian",
                    WorkingHours = "08:00 - 17:30, tu thu Hai den thu Sau",
                    RestTime = "Nghi trua 12:00 - 13:30",
                    SalaryPaymentMethod = "Chuyen khoan",
                    SalaryPaymentDate = "Ngay 05 hang thang",
                    AllowanceDescription = "Phu cap va thuong theo chinh sach cong ty.",
                    BonusPolicy = "Thuong KPI va thuong du an theo quy che luong thuong.",
                    KpiBonusTargetAmount = 5000000m,
                    KpiBonusPolicyCode = "KPI_BONUS",
                    KpiPayoutFormula = "KPI bonus = KPI target * KPI score / 100",
                    InsurancePolicy = "Dong BHXH, BHYT, BHTN theo quy dinh.",
                    EmployeeObligations = "Tuan thu noi quy lao dong va bao mat thong tin.",
                    EmployerObligations = "Tra luong va dam bao quyen loi theo hop dong.",
                    ConfidentialityClause = "Bao mat du lieu khach hang va tai lieu noi bo.",
                    IntellectualPropertyClause = "San pham tao ra trong cong viec thuoc quyen so huu cong ty.",
                    TerminationClause = "Cham dut hop dong theo quy dinh phap luat.",
                    DisputeResolutionClause = "Uu tien thuong luong, sau do xu ly theo phap luat.",
                    DocumentDocFilePath = $"/contract-documents/contracts/{entity.Id}/{number}.doc",
                    DocumentPdfFilePath = $"/contract-documents/contracts/{entity.Id}/{number}.pdf",
                    IssuedAt = entity.IssuedAt,
                    EmployeeSignedAt = status == ContractStatus.Active ? DateTime.UtcNow.AddMonths(-1) : null,
                    EmployerSignedAt = status == ContractStatus.Active ? DateTime.UtcNow.AddMonths(-1) : null
                });
            }

            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsureContractAddendumAsync(MyDbContext context, Contract contract, string number, AddendumStatus status, CancellationToken ct)
        {
            if (await context.ContractAddendums.AnyAsync(a => a.AddendumNumber == number, ct)) return;

            context.ContractAddendums.Add(new ContractAddendum
            {
                ContractId = contract.Id,
                AddendumNumber = number,
                AddendumType = ContractAddendumType.SalaryAdjustment,
                BaseContractNumberSnapshot = contract.ContractNumber,
                BaseContractStartDateSnapshot = contract.StartDate,
                BaseContractEndDateSnapshot = contract.EndDate,
                NewBasicSalary = contract.BasicSalary + 2500000m,
                NewInsuranceSalary = contract.InsuranceSalary + 2500000m,
                ChangedContentSummary = "Dieu chinh luong co ban va luong dong bao hiem.",
                UnchangedTerms = "Cac dieu khoan con lai cua hop dong goc giu nguyen.",
                LegalDocumentNumber = number,
                DocumentTemplateCode = "CONTRACT_ADDENDUM",
                DocumentDocFilePath = $"/contract-documents/addendums/demo/{number}.doc",
                DocumentPdfFilePath = $"/contract-documents/addendums/demo/{number}.pdf",
                EffectiveDate = DateTime.UtcNow.AddDays(10),
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        private static async Task EnsureEmploymentHistoryAsync(MyDbContext context, Employee employee, HistoryType type, string oldValue, string newValue, DateTime effectiveDate, Employee approver, CancellationToken ct)
        {
            if (await context.EmploymentHistories.AnyAsync(h => h.EmployeeId == employee.Id && h.Type == type && h.EffectiveDate == effectiveDate.Date, ct)) return;

            context.EmploymentHistories.Add(new EmploymentHistory
            {
                EmployeeId = employee.Id,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                EffectiveDate = effectiveDate.Date,
                ApprovedBy = approver.Id,
                ChangeDate = DateTime.UtcNow.AddMonths(-4)
            });
        }

        private static async Task<WorkShift> EnsureWorkShiftAsync(MyDbContext context, Department department, CancellationToken ct)
        {
            var entity = await context.WorkShifts.FirstOrDefaultAsync(s => s.DeptId == department.Id, ct);
            if (entity != null) return entity;

            entity = new WorkShift
            {
                DeptId = department.Id,
                ShiftName = "Demo Ca hanh chinh",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 30, 0),
                BreakStartTime = new TimeSpan(12, 0, 0),
                BreakEndTime = new TimeSpan(13, 30, 0),
                LateThresholdMins = 10,
                EarlyLeaveThresholdMins = 10,
                IsActive = true
            };
            context.WorkShifts.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task<LeaveType> EnsureLeaveTypeAsync(MyDbContext context, CancellationToken ct)
        {
            var entity = await context.LeaveTypes.FirstOrDefaultAsync(l => l.TypeName == "Demo Phep nam", ct);
            if (entity != null) return entity;

            entity = new LeaveType
            {
                TypeName = "Demo Phep nam",
                Category = LeaveCategory.AnnualPaid,
                IsPaid = true,
                CountsAsWorkday = true,
                DeductAnnualLeave = true
            };
            context.LeaveTypes.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsureLeaveBalanceAsync(MyDbContext context, Employee employee, LeaveType leaveType, short year, CancellationToken ct)
        {
            if (await context.LeaveBalances.AnyAsync(l => l.EmployeeId == employee.Id && l.LeaveTypeId == leaveType.Id && l.Year == year, ct)) return;

            context.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                Year = year,
                TotalDays = 12,
                UsedDays = 2
            });
        }

        private static async Task EnsureAttendanceLogAsync(MyDbContext context, Employee employee, WorkShift shift, DateTime workDate, int lateMinutes, CancellationToken ct)
        {
            if (await context.AttendanceLogs.AnyAsync(a => a.EmployeeId == employee.Id && a.WorkDate == workDate, ct)) return;

            context.AttendanceLogs.Add(new AttendanceLog
            {
                EmployeeId = employee.Id,
                ShiftId = shift.Id,
                WorkDate = workDate,
                CheckIn = workDate.AddHours(8).AddMinutes(lateMinutes),
                CheckOut = workDate.AddHours(17).AddMinutes(35),
                IpAddress = "192.168.1.100",
                GpsLat = 21.004118m,
                GpsLong = 105.843381m,
                Status = lateMinutes > shift.LateThresholdMins ? AttendanceStatus.Late : AttendanceStatus.Valid
            });
        }

        private static async Task RemoveCollaboratorInternalTimekeepingAsync(MyDbContext context, Employee collaborator, CancellationToken ct)
        {
            var attendanceLogs = await context.AttendanceLogs
                .Where(a => a.EmployeeId == collaborator.Id)
                .ToListAsync(ct);
            if (attendanceLogs.Any())
                context.AttendanceLogs.RemoveRange(attendanceLogs);

            var dailySummaries = await context.AttendanceDailySummaries
                .Where(a => a.EmployeeId == collaborator.Id)
                .ToListAsync(ct);
            if (dailySummaries.Any())
                context.AttendanceDailySummaries.RemoveRange(dailySummaries);

            var monthlySummaries = await context.AttendanceSummaries
                .Where(a => a.EmployeeId == collaborator.Id)
                .ToListAsync(ct);
            if (monthlySummaries.Any())
                context.AttendanceSummaries.RemoveRange(monthlySummaries);

            var overtimeRequests = await context.OvertimeRequests
                .Where(o => o.EmployeeId == collaborator.Id)
                .ToListAsync(ct);
            if (overtimeRequests.Any())
                context.OvertimeRequests.RemoveRange(overtimeRequests);
        }

        private static async Task EnsureDailySummaryAsync(MyDbContext context, Employee employee, DateTime workDate, string period, bool adjusted, AttendancePayrollApprovalStatus status, CancellationToken ct)
        {
            if (await context.AttendanceDailySummaries.AnyAsync(a => a.EmployeeId == employee.Id && a.WorkDate == workDate, ct)) return;

            context.AttendanceDailySummaries.Add(new AttendanceDailySummary
            {
                EmployeeId = employee.Id,
                WorkDate = workDate,
                FirstCheckIn = workDate.AddHours(8).AddMinutes(adjusted ? 18 : 3),
                LastCheckOut = workDate.AddHours(17).AddMinutes(35),
                WorkingMinutes = 480,
                LateMinutes = adjusted ? 18 : 0,
                EarlyLeaveMinutes = 0,
                OvertimeMinutes = adjusted ? 45 : 0,
                WorkdayValue = 1,
                AttendanceStatus = AttendanceDailyStatus.Present,
                ApprovalStatus = status,
                IsManualAdjusted = adjusted,
                AdjustmentReason = adjusted ? "Demo dieu chinh do nhan vien quen check-in dung gio." : null,
                PayrollPeriod = period,
                GeneratedAt = DateTime.UtcNow.AddHours(-8)
            });
        }

        private static async Task EnsureAttendanceSummaryAsync(MyDbContext context, Employee employee, int month, short year, AttendancePayrollApprovalStatus status, int submittedBy, int? approvedBy, bool locked, CancellationToken ct)
        {
            var entity = await context.AttendanceSummaries.FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Month == month && a.Year == year, ct);
            if (entity == null)
            {
                entity = new AttendanceSummary
                {
                    EmployeeId = employee.Id,
                    Month = (byte)month,
                    Year = year,
                    WorkDays = 22,
                    WorkedMinutes = 10560,
                    PayableWorkHours = 176,
                    LateMinutes = 18,
                    EarlyLeaveMinutes = 0,
                    ActualOtMinutes = 120,
                    GeneratedAt = DateTime.UtcNow.AddHours(-6)
                };
                context.AttendanceSummaries.Add(entity);
            }

            entity.ApprovalStatus = status;
            var submitted = status != AttendancePayrollApprovalStatus.Draft;
            entity.SubmittedByAccountId = submitted ? submittedBy : null;
            entity.SubmittedAt = submitted ? DateTime.UtcNow.AddHours(-5) : null;
            entity.ApprovedByAccountId = approvedBy;
            entity.ApprovedAt = approvedBy.HasValue ? DateTime.UtcNow.AddHours(-3) : null;
            entity.LockedByAccountId = locked ? approvedBy : null;
            entity.LockedAt = locked ? DateTime.UtcNow.AddHours(-1) : null;
            entity.IsPayrollLocked = locked;
            entity.PeriodNote = status == AttendancePayrollApprovalStatus.Draft
                ? "Demo bang cong moi tong hop, HR co the tu gui duyet/chot."
                : locked ? "Demo bang cong da chot de tinh luong." : "Demo bang cong cho HR kiem tra.";
        }

        private static async Task EnsureLeaveRequestAsync(MyDbContext context, Employee employee, LeaveType leaveType, LeaveRequestStatus status, string period, CancellationToken ct)
        {
            var start = DateTime.UtcNow.Date.AddDays(4);
            if (await context.LeaveRequests.AnyAsync(l => l.EmployeeId == employee.Id && l.StartDate == start && l.Status == status, ct)) return;

            context.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                StartDate = start,
                EndDate = start.AddDays(1),
                Reason = "Demo nghi phep de giai quyet viec gia dinh.",
                Status = status,
                DeadlineAt = DateTime.UtcNow.AddHours(30),
                PayrollPeriod = period
            });
        }

        private static async Task EnsureOvertimeRequestAsync(MyDbContext context, Employee employee, int requestedByAccountId, OvertimeRequestStatus status, string period, CancellationToken ct)
        {
            var workDate = DateTime.UtcNow.Date.AddDays(-1);
            if (await context.OvertimeRequests.AnyAsync(o => o.EmployeeId == employee.Id && o.WorkDate == workDate && o.Status == status, ct)) return;

            context.OvertimeRequests.Add(new OvertimeRequest
            {
                EmployeeId = employee.Id,
                RequestedByAccountId = requestedByAccountId,
                WorkDate = workDate,
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                StartAt = workDate.AddHours(18),
                EndAt = workDate.AddHours(20),
                Reason = "Demo xu ly go-live du an ERP.",
                ProjectCode = "HICAS-ERP",
                Status = status,
                ApprovedMinutes = status == OvertimeRequestStatus.Approved ? 120 : 0,
                ActualOtMinutes = 120,
                PayrollPeriod = period,
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            });
        }

        private static async Task<KpiImportBatch> EnsureKpiBatchAsync(MyDbContext context, Department dept, int importedBy, string period, CancellationToken ct)
        {
            var entity = await context.KpiImportBatches.FirstOrDefaultAsync(b => b.Period == period && b.DeptId == dept.Id && b.FileName == "demo-kpi-import.xlsx", ct);
            if (entity != null) return entity;

            entity = new KpiImportBatch
            {
                Period = period,
                DeptId = dept.Id,
                ImportedByAccountId = importedBy,
                FileName = "demo-kpi-import.xlsx",
                TotalRows = 9,
                SuccessRows = 9,
                ErrorRows = 0,
                Status = ImportBatchStatus.Completed
            };
            context.KpiImportBatches.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task<PerformanceReview> EnsurePerformanceReviewAsync(
            MyDbContext context,
            Employee employee,
            Department dept,
            KpiImportBatch batch,
            int createdBy,
            int reviewerId,
            string period,
            ReviewStatus status,
            decimal score,
            CancellationToken ct)
        {
            var entity = await context.PerformanceReviews
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.Period == period, ct);
            if (entity == null)
            {
                entity = new PerformanceReview
                {
                    EmployeeId = employee.Id,
                    DeptId = dept.Id,
                    ImportBatchId = batch.Id,
                    CreatedByAccountId = createdBy,
                    ReviewerAccountId = reviewerId,
                    Period = period,
                    Status = status,
                    TotalWeight = 100,
                    TotalScore = score,
                    FinalRating = score >= 85 ? "A" : null,
                    FinalComment = score > 0 ? "Demo ket qua KPI da duoc truong phong danh gia." : null,
                    ScoringVersion = "WeightedV2",
                    ReviewDeadline = DateTime.UtcNow.AddDays(5),
                    FinalizedAt = status is ReviewStatus.Evaluated or ReviewStatus.Approved ? DateTime.UtcNow.AddHours(-4) : null
                };
                context.PerformanceReviews.Add(entity);
                await context.SaveChangesAsync(ct);
            }

            entity.Status = status;
            entity.TotalScore = score;

            if (!entity.Details.Any())
            {
                context.PerformanceDetails.AddRange(
                    KpiDetail(entity.Id, "DEMO-KPI-001", "Hoan thanh task dung han", 40, 100, 92, 90, "Demo minh chung task"),
                    KpiDetail(entity.Id, "DEMO-KPI-002", "Chat luong xu ly", 35, 100, 88, 85, "Demo chat luong ban giao"),
                    KpiDetail(entity.Id, "DEMO-KPI-003", "Tuan thu quy trinh", 25, 100, 96, 90, "Demo tuan thu SLA"));
            }

            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static PerformanceDetail KpiDetail(int reviewId, string code, string name, int weight, decimal target, decimal actual, decimal managerScore, string comment)
        {
            var achieved = target == 0 ? managerScore : actual / target * 100;
            var final = Math.Max(0, weight * managerScore / 100);
            return new PerformanceDetail
            {
                ReviewId = reviewId,
                KpiCode = code,
                KpiName = name,
                WeightPercent = weight,
                TargetValue = target,
                ActualValue = actual,
                Unit = "%",
                EmployeeSelfPercent = achieved,
                AchievedPercent = achieved,
                ManagerScore = managerScore,
                FinalPoint = final,
                EmployeeComment = comment,
                ManagerComment = "Demo diem chot cua truong phong.",
                EvidencePath = "/uploads/task-evidence/demo-kpi.pdf"
            };
        }

        private static async Task EnsurePenaltyRecordAsync(MyDbContext context, Employee employee, PerformanceReview? review, int createdBy, PenaltyRecordStatus status, CancellationToken ct)
        {
            if (await context.PenaltyRecords.AnyAsync(p => p.EmployeeId == employee.Id && p.RuleCode == "DEMO_POLICY_VIOLATION", ct)) return;

            context.PenaltyRecords.Add(new PenaltyRecord
            {
                EmployeeId = employee.Id,
                Period = $"{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Year}",
                SourceType = PenaltySourceType.Manual,
                RuleCode = "DEMO_POLICY_VIOLATION",
                PenaltyPoint = 2,
                Reason = "Demo vi pham quy trinh xu ly ticket.",
                Status = status,
                OccurredAt = DateTime.UtcNow.AddDays(-3),
                ViolationType = ViolationType.ProcessViolation,
                Severity = PenaltySeverity.Medium,
                AffectsAttendance = false,
                AffectsPerformance = true,
                AffectsPersonnelDecision = true,
                CreatedBySystem = false,
                CreatedByAccountId = createdBy,
                HRNote = "Can nhan vien giai trinh truoc khi ap dung.",
                EvidenceFilePath = "/uploads/personnel-change-evidence/demo-violation.pdf",
                PerformanceReviewId = review?.Id,
                ReviewedAt = status == PenaltyRecordStatus.Approved ? DateTime.UtcNow.AddHours(-8) : null
            });
        }

        private static async Task<Training> EnsureTrainingAsync(MyDbContext context, Employee employee, Department dept, Employee manager, TrainingStatus status, CancellationToken ct)
        {
            var entity = await context.Trainings.FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.CourseName == "Demo Onboarding HICAS", ct);
            if (entity != null) return entity;

            entity = new Training
            {
                EmployeeId = employee.Id,
                DeptId = dept.Id,
                ManagerId = manager.Id,
                CourseName = "Demo Onboarding HICAS",
                TrainingType = "Onboarding",
                Status = status,
                StartedAt = DateTime.UtcNow.AddDays(-20),
                EvaluationDeadline = DateTime.UtcNow.AddDays(3),
                Deadline = DateTime.UtcNow.AddDays(3)
            };
            context.Trainings.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsureTaskAsync(MyDbContext context, Employee employee, Department dept, int createdBy, Training? training, string title, TaskStatus status, int progress, CancellationToken ct)
        {
            if (await context.Tasks.AnyAsync(t => t.Title == title && t.AssignedTo == employee.Id, ct)) return;

            context.Tasks.Add(new WorkTask
            {
                Title = title,
                Description = "Demo cong viec de chup man hinh tien do, minh chung va phe duyet.",
                TaskType = training == null ? TaskType.Project : TaskType.SelfStudy,
                DeptId = dept.Id,
                AssignedTo = employee.Id,
                CreatedByAccountId = createdBy,
                TrainingId = training?.Id,
                ProgressPercent = progress,
                BonusAmount = 0m,
                ActualBonus = 0m,
                Status = status,
                EvidencePath = "/uploads/task-evidence/demo-task.pdf",
                Deadline = DateTime.UtcNow.AddDays(4),
                ReviewDeadline = DateTime.UtcNow.AddDays(5),
                SubmittedAt = status == TaskStatus.PendingReview ? DateTime.UtcNow.AddHours(-5) : null
            });
        }

        private static async Task<PayrollFormula> EnsurePayrollFormulaAsync(MyDbContext context, int createdBy, int approvedBy, CancellationToken ct)
        {
            var entity = await context.PayrollFormulas
                .Include(f => f.Lines)
                .FirstOrDefaultAsync(f => f.FormulaCode == "DEMO_PAYROLL_SCREENSHOT", ct);
            if (entity == null)
            {
                entity = new PayrollFormula
                {
                    FormulaCode = "DEMO_PAYROLL_SCREENSHOT",
                    FormulaName = "Demo cong thuc luong chinh thuc",
                    Expression = "gross_income = base + allowance + bonus - deductions",
                    Version = 1,
                    VersionCode = "DEMO_2026_V1",
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    Status = FormulaStatus.Draft,
                    IsActive = false,
                    CreatedByAccountId = createdBy,
                    ReviewNote = "Demo cong thuc dang o ban nhap de HR tu gui duyet."
                };
                context.PayrollFormulas.Add(entity);
                await context.SaveChangesAsync(ct);
            }

            if (!entity.Lines.Any())
            {
                context.PayrollFormulaLines.AddRange(
                    FormulaLine(entity.Id, "BASE_SALARY_ACTUAL", "contract_segment_salary_amount", 10, true, true, true, false),
                    FormulaLine(entity.Id, "INTERN_ALLOWANCE", "intern_allowance_amount", 75, true, true, false, false),
                    FormulaLine(entity.Id, "KPI_BONUS", "kpi_bonus_amount * kpi_score / 100", 80, true, true, false, false),
                    FormulaLine(entity.Id, "PROJECT_BONUS", "project_bonus_amount", 87, true, true, false, false),
                    FormulaLine(entity.Id, "EMPLOYEE_INSURANCE", "insurance_salary * employee_insurance_rate", 200, false, false, false, true),
                    FormulaLine(entity.Id, "PIT", "pit(pit_tax_base)", 210, false, false, false, true));
            }

            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static PayrollFormulaLine FormulaLine(int formulaId, string code, string expression, int order, bool gross, bool taxable, bool insurance, bool deduction)
        {
            return new PayrollFormulaLine
            {
                PayrollFormulaId = formulaId,
                ComponentCode = code,
                Expression = expression,
                CalculationOrder = order,
                IsGrossComponent = gross,
                IsTaxable = taxable,
                IsInsuranceBased = insurance,
                IsDeduction = deduction,
                IsSnapshotRequired = true
            };
        }

        private static async Task EnsureEmployeeSalaryComponentAsync(MyDbContext context, Employee employee, string code, string name, SalaryComponentGroup group, decimal amount, CancellationToken ct)
        {
            var type = await context.SalaryComponentTypes.FirstOrDefaultAsync(s => s.Code == code, ct);
            if (type == null)
            {
                type = new SalaryComponentType
                {
                    Code = code,
                    Name = name,
                    ComponentGroup = group,
                    IsIncome = group != SalaryComponentGroup.Deduction,
                    IsDeduction = group == SalaryComponentGroup.Deduction,
                    IsTaxable = group != SalaryComponentGroup.Allowance,
                    IsBonus = group == SalaryComponentGroup.Bonus,
                    IsAllowance = group == SalaryComponentGroup.Allowance,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    VersionCode = "DEMO_2026",
                    Status = PolicyVersionStatus.Active,
                    IsActive = true
                };
                context.SalaryComponentTypes.Add(type);
                await context.SaveChangesAsync(ct);
            }

            var existing = await context.EmployeeSalaryComponents
                .FirstOrDefaultAsync(e => e.EmployeeId == employee.Id && e.SalaryComponentTypeId == type.Id, ct);
            if (existing != null)
            {
                existing.Amount = amount;
                existing.IsActive = true;
                existing.SourceReference = "DEMO_SCREENSHOT_SEED";
                return;
            }

            context.EmployeeSalaryComponents.Add(new EmployeeSalaryComponent
            {
                EmployeeId = employee.Id,
                SalaryComponentTypeId = type.Id,
                Amount = amount,
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true,
                SourceReference = "DEMO_SCREENSHOT_SEED"
            });
        }

        private static async Task<Payroll> EnsurePayrollAsync(MyDbContext context, Employee employee, int month, int year, string period, PayrollStatus status, int calculatedBy, int? approvedBy, bool locked, CancellationToken ct)
        {
            var entity = await context.Payrolls.FirstOrDefaultAsync(p => p.EmployeeId == employee.Id && p.Month == month && p.Year == year, ct);
            if (entity == null)
            {
                entity = new Payroll
                {
                    EmployeeId = employee.Id,
                    Month = (byte)month,
                    Year = (short)year,
                    Period = period,
                    BaseSalary = 18000000m,
                    BaseSalaryActual = 18000000m,
                    GrossSalary = 25870000m,
                    GrossIncome = 25870000m,
                    TotalAllowance = 730000m,
                    TotalBonus = 7870000m,
                    InsuranceSalary = 18000000m,
                    InsuranceDeduction = 1890000m,
                    EmployeeInsuranceAmount = 1890000m,
                    EmployerContributionAmount = 3870000m,
                    TaxDeductionPersonal = 11000000m,
                    TaxDeductionFamily = 4400000m,
                    TaxableGrossIncome = 23980000m,
                    TaxableIncome = 8580000m,
                    PitAmount = 620000m,
                    OtherDeductions = 0,
                    NetSalary = 23360000m,
                    TotalCompanyCost = 29740000m,
                    ActualWorkDays = 22,
                    ActualWorkHours = 176,
                    ActualOtMinutes = 120,
                    CalculatedByAccountId = calculatedBy,
                    CalculatedAt = DateTime.UtcNow.AddHours(-4),
                    Status = status
                };
                context.Payrolls.Add(entity);
            }

            entity.Status = status;
            var submitted = status is PayrollStatus.PendingApproval
                or PayrollStatus.Approved
                or PayrollStatus.Locked
                or PayrollStatus.Finalized
                or PayrollStatus.Paid;
            entity.SubmittedAt = submitted ? DateTime.UtcNow.AddHours(-3) : null;
            entity.SubmittedByAccountId = submitted ? calculatedBy : null;
            entity.ApprovedByAccountId = approvedBy;
            entity.ApprovedAt = approvedBy.HasValue ? DateTime.UtcNow.AddHours(-2) : null;
            entity.LockedByAccountId = locked ? approvedBy : null;
            entity.LockedAt = locked ? DateTime.UtcNow.AddHours(-1) : null;
            entity.ReviewNote = status switch
            {
                PayrollStatus.Calculated => "Demo bang luong da tinh, HR can kiem tra va tu gui duyet.",
                PayrollStatus.PendingApproval => "Demo bang luong cho giam doc duyet.",
                _ when locked => "Demo bang luong da chot va phat hanh phieu luong.",
                _ => "Demo bang luong o buoc dau."
            };

            await context.SaveChangesAsync(ct);
            return entity;
        }

        private static async Task EnsurePayrollDetailAsync(
            MyDbContext context,
            Payroll payroll,
            string code,
            string name,
            decimal amount,
            bool income,
            bool deduction,
            CancellationToken ct,
            bool? taxable = null,
            bool? insuranceBased = null)
        {
            var isTaxable = taxable ?? income;
            var isInsuranceBased = insuranceBased ?? code == "BASE_SALARY_ACTUAL";
            var detail = await context.PayrollDetails
                .FirstOrDefaultAsync(d => d.PayrollId == payroll.Id && d.ComponentCode == code, ct);

            if (detail == null)
            {
                detail = new PayrollDetail
                {
                    PayrollId = payroll.Id,
                    ComponentCode = code,
                    ComponentName = name,
                    CalculationMethod = "Demo",
                    Note = "Demo salary slip component"
                };
                context.PayrollDetails.Add(detail);
            }

            detail.ComponentName = name;
            detail.Amount = amount;
            detail.TaxableAmount = isTaxable ? amount : 0;
            detail.InsuranceBaseAmount = isInsuranceBased ? amount : 0;
            detail.IsIncome = income;
            detail.IsDeduction = deduction;
            detail.IsTaxable = isTaxable;
            detail.IsInsuranceBased = isInsuranceBased;
        }

        private static async Task EnsureProjectBonusAsync(MyDbContext context, DemoUsers demo, int month, int year, string period, CancellationToken ct)
        {
            var batch = await context.ProjectBonusImportBatches
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(b => b.PayrollPeriod == period && b.FileName == "demo-project-bonus.xlsx", ct);
            if (batch == null)
            {
                batch = new ProjectBonusImportBatch
                {
                    PeriodMonth = (byte)month,
                    PeriodYear = (short)year,
                    PayrollPeriod = period,
                    FileName = "demo-project-bonus.xlsx",
                    UploadedByAccountId = demo.HrAccount.Id,
                    Status = ProjectBonusImportStatus.Draft,
                    TotalRows = 4,
                    ValidRows = 4,
                    ErrorRows = 0,
                    TotalAmount = 18500000m,
                    Note = "Demo batch thuong du an dang o ban nhap de HR tu gui duyet.",
                    CreatedAt = DateTime.UtcNow.AddHours(-5)
                };
                context.ProjectBonusImportBatches.Add(batch);
                await context.SaveChangesAsync(ct);
            }

            await EnsureProjectBonusLineAsync(context, batch, 1, demo.Employee, "HICAS-ERP", "Go-live ERP", 3500000m, ct);
            await EnsureProjectBonusLineAsync(context, batch, 2, demo.Manager, "HICAS-BIM", "BIM Platform", 3000000m, ct);
            await EnsureProjectBonusLineAsync(context, batch, 3, demo.Director, "HICAS-OPS", "Dieu phoi chuong trinh chuyen doi so", 8000000m, ct);
            await EnsureProjectBonusLineAsync(context, batch, 4, demo.Hr, "HICAS-HRM", "Trien khai HRM noi bo", 4000000m, ct);

            batch.Status = ProjectBonusImportStatus.Draft;
            batch.TotalRows = 4;
            batch.ValidRows = 4;
            batch.ErrorRows = 0;
            batch.TotalAmount = 18500000m;
        }

        private static async Task EnsureProjectBonusLineAsync(
            MyDbContext context,
            ProjectBonusImportBatch batch,
            int row,
            Employee employee,
            string projectCode,
            string projectName,
            decimal amount,
            CancellationToken ct)
        {
            var line = await context.ProjectBonusImportLines
                .FirstOrDefaultAsync(l => l.BatchId == batch.Id && l.EmployeeId == employee.Id && l.ProjectCode == projectCode, ct);
            if (line == null)
            {
                context.ProjectBonusImportLines.Add(ProjectBonusLine(batch.Id, row, employee, projectCode, projectName, amount));
                return;
            }

            line.RowNumber = row;
            line.EmployeeCodeSnapshot = employee.EmployeeCode;
            line.EmployeeNameSnapshot = employee.FullName;
            line.ProjectName = projectName;
            line.BonusAmount = amount;
            line.Taxable = true;
            line.InsuranceContributable = false;
            line.Reason = "Demo thuong du an da nghiem thu.";
            line.ValidationStatus = ProjectBonusLineValidationStatus.Valid;
            line.ErrorMessage = null;
        }

        private static ProjectBonusImportLine ProjectBonusLine(int batchId, int row, Employee employee, string projectCode, string projectName, decimal amount)
        {
            return new ProjectBonusImportLine
            {
                BatchId = batchId,
                RowNumber = row,
                EmployeeId = employee.Id,
                EmployeeCodeSnapshot = employee.EmployeeCode,
                EmployeeNameSnapshot = employee.FullName,
                ProjectCode = projectCode,
                ProjectName = projectName,
                BonusAmount = amount,
                Taxable = true,
                InsuranceContributable = false,
                Reason = "Demo thuong du an da nghiem thu.",
                ValidationStatus = ProjectBonusLineValidationStatus.Valid
            };
        }

        private static async Task EnsureExternalTimesheetAsync(MyDbContext context, DemoUsers demo, int month, int year, string period, CancellationToken ct)
        {
            var import = await context.ExternalTimesheetImports
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.PayrollPeriod == period && i.FileName == "demo-external-timesheet.csv", ct);
            if (import == null)
            {
                import = new ExternalTimesheetImport
                {
                    SourceSystem = "Demo Portal",
                    ImportMonth = (byte)month,
                    ImportYear = (short)year,
                    PayrollPeriod = period,
                    FileName = "demo-external-timesheet.csv",
                    ImportedByAccountId = demo.HrAccount.Id,
                    Status = ExternalTimesheetImportStatus.Draft,
                    TotalRows = 2,
                    ValidRows = 2,
                    ErrorRows = 0,
                    TotalHours = 36,
                    TotalAmount = 5400000m,
                    Note = "Demo gio cong cong tac vien dang o ban nhap de HR tu gui duyet."
                };
                context.ExternalTimesheetImports.Add(import);
                await context.SaveChangesAsync(ct);
            }

            if (!import.Lines.Any())
            {
                context.ExternalTimesheetLines.AddRange(
                    ExternalLine(import.Id, 1, demo.Collaborator, DateTime.UtcNow.Date.AddDays(-5), 20, 150000m),
                    ExternalLine(import.Id, 2, demo.Collaborator, DateTime.UtcNow.Date.AddDays(-3), 16, 150000m));
            }
        }

        private static ExternalTimesheetLine ExternalLine(int importId, int row, Employee employee, DateTime date, decimal hours, decimal rate)
        {
            return new ExternalTimesheetLine
            {
                ImportId = importId,
                RowNumber = row,
                CollaboratorEmployeeId = employee.Id,
                CollaboratorCode = employee.EmployeeCode,
                CollaboratorNameSnapshot = employee.FullName,
                WorkDate = date,
                ProjectCode = "HICAS-ERP",
                TaskCode = "DEMO-TASK",
                ApprovedHours = hours,
                HourlyRate = rate,
                Amount = hours * rate,
                ValidationStatus = ProjectBonusLineValidationStatus.Valid,
                Note = "Demo gio cong da duyet."
            };
        }

        private static async Task EnsurePersonnelChangeAsync(
            MyDbContext context,
            DemoUsers demo,
            DemoOrg org,
            PersonnelChangeType type,
            PersonnelChangeStatus status,
            int? performanceReviewId,
            int? penaltyRecordId,
            CancellationToken ct)
        {
            var reason = type switch
            {
                PersonnelChangeType.Promotion => "Demo de xuat thang tien do dat KPI tot.",
                PersonnelChangeType.ConvertToOfficial => "Demo chuyen nhan su sang chinh thuc.",
                PersonnelChangeType.SeniorAppointment => "Demo bo nhiem nhan su cap cao.",
                PersonnelChangeType.InternalTransfer => "Demo thuyen chuyen sang phong San pham.",
                PersonnelChangeType.VoluntaryTermination => "Demo nhan vien chu dong xin nghi viec.",
                PersonnelChangeType.Dismissal => "Demo ho so ky luat/sa thai co minh chung.",
                _ => "Demo bien dong nhan su."
            };

            if (await context.PersonnelChangeRequests.AnyAsync(p => p.ChangeType == type && p.Reason == reason, ct)) return;

            var requiresEmployeeConsent = type is PersonnelChangeType.SeniorAppointment or PersonnelChangeType.InternalTransfer;
            var contractFlowStarted = status is PersonnelChangeStatus.PendingContractFlow
                or PersonnelChangeStatus.ContractNegotiating
                or PersonnelChangeStatus.ContractAccepted
                or PersonnelChangeStatus.ReadyToExecute
                or PersonnelChangeStatus.Completed;
            var hasDirectorDecision = status is PersonnelChangeStatus.ApprovedByDirector
                or PersonnelChangeStatus.PendingContractFlow
                or PersonnelChangeStatus.ContractNegotiating
                or PersonnelChangeStatus.ContractAccepted
                or PersonnelChangeStatus.PendingDecisionIssuance
                or PersonnelChangeStatus.ReadyToExecute
                or PersonnelChangeStatus.Completed;
            var dismissalNotified = type == PersonnelChangeType.Dismissal &&
                status is PersonnelChangeStatus.PendingEmployeeExplanation
                    or PersonnelChangeStatus.PendingDirectorApproval
                    or PersonnelChangeStatus.ApprovedByDirector
                    or PersonnelChangeStatus.PendingContractFlow
                    or PersonnelChangeStatus.ContractAccepted
                    or PersonnelChangeStatus.ReadyToExecute
                    or PersonnelChangeStatus.Completed;

            var request = new PersonnelChangeRequest
            {
                EmployeeId = type == PersonnelChangeType.SeniorAppointment ? demo.Manager.Id : demo.Employee.Id,
                ChangeType = type,
                PromotionType = type == PersonnelChangeType.ConvertToOfficial
                    ? PersonnelChangePromotionType.ConvertToOfficial
                    : type == PersonnelChangeType.Promotion
                        ? PersonnelChangePromotionType.JobLevelPromotion
                        : null,
                Status = status,
                RequestedByAccountId = type == PersonnelChangeType.VoluntaryTermination ? demo.EmployeeAccount.Id : demo.HrAccount.Id,
                RequestedAt = DateTime.UtcNow.AddDays(-2),
                Reason = reason,
                EffectiveDate = DateTime.UtcNow.AddDays(14),
                CurrentDepartmentId = org.Tech.Id,
                CurrentPositionId = org.EngineerPosition.Id,
                CurrentManagerId = demo.Manager.Id,
                CurrentJobLevelId = org.StaffLevel.Id,
                CurrentEmployeeType = EmployeeType.Official,
                NewDepartmentId = type == PersonnelChangeType.InternalTransfer ? org.Product.Id : org.Tech.Id,
                NewPositionId = type == PersonnelChangeType.SeniorAppointment ? org.ManagerPosition.Id : org.EngineerPosition.Id,
                NewManagerId = demo.Manager.Id,
                NewJobLevelId = type is PersonnelChangeType.Promotion or PersonnelChangeType.SeniorAppointment ? org.SeniorLevel.Id : org.StaffLevel.Id,
                NewEmployeeType = type == PersonnelChangeType.ConvertToOfficial ? EmployeeType.Official : null,
                RequiresEmployeeConsent = requiresEmployeeConsent,
                EmployeeConsentStatus = requiresEmployeeConsent
                    ? status == PersonnelChangeStatus.PendingEmployeeConsent ? PersonnelChangeConsentStatus.Pending : PersonnelChangeConsentStatus.NotRequired
                    : PersonnelChangeConsentStatus.NotRequired,
                RequiresContractFlow = type is PersonnelChangeType.Promotion or PersonnelChangeType.SeniorAppointment or PersonnelChangeType.Dismissal,
                ContractFlowType = type == PersonnelChangeType.Dismissal ? PersonnelChangeContractFlowType.ContractTermination : PersonnelChangeContractFlowType.ContractAddendum,
                ContractFlowStatus = contractFlowStarted
                    ? status == PersonnelChangeStatus.ReadyToExecute || status == PersonnelChangeStatus.Completed ? "Accepted" : "Pending"
                    : null,
                RequiresDirectorApproval = true,
                DirectorApprovedByAccountId = hasDirectorDecision ? demo.DirectorAccount.Id : null,
                DirectorApprovedAt = hasDirectorDecision ? DateTime.UtcNow.AddHours(-6) : null,
                RequiresHRProcessing = true,
                HRAssignedAccountId = demo.HrAccount.Id,
                HRNote = "Demo ho so bien dong de chup man hinh.",
                HRProcessedAt = status != PersonnelChangeStatus.PendingHRReview ? DateTime.UtcNow.AddHours(-10) : null,
                EmployeeNotifiedAt = dismissalNotified ? DateTime.UtcNow.AddDays(-1) : null,
                ResponseDeadlineAt = type == PersonnelChangeType.Dismissal ? DateTime.UtcNow.AddDays(2) : null,
                EvidenceFilePath = type == PersonnelChangeType.Dismissal && dismissalNotified ? "/uploads/personnel-change-evidence/demo-dismissal.pdf" : null,
                EmployeeExplanation = type == PersonnelChangeType.Dismissal && status == PersonnelChangeStatus.PendingEmployeeExplanation ? "Demo nhan vien dang giai trinh." : null,
                SourcePerformanceReviewId = performanceReviewId,
                SourcePenaltyRecordId = penaltyRecordId,
                DecisionNumber = status is PersonnelChangeStatus.ReadyToExecute or PersonnelChangeStatus.Completed ? $"DEMO-QD-{type}-2026" : null,
                DecisionFilePath = status is PersonnelChangeStatus.ReadyToExecute or PersonnelChangeStatus.Completed ? $"/uploads/decisions/demo-{type}.docx" : null,
                DecisionIssuedAt = status is PersonnelChangeStatus.ReadyToExecute or PersonnelChangeStatus.Completed ? DateTime.UtcNow.AddHours(-2) : null,
                CompletedAt = status == PersonnelChangeStatus.Completed ? DateTime.UtcNow.AddHours(-1) : null,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow
            };

            context.PersonnelChangeRequests.Add(request);
            await context.SaveChangesAsync(ct);

            context.PersonnelChangeHistories.Add(new PersonnelChangeHistory
            {
                RequestId = request.Id,
                Action = "Created",
                OldStatus = null,
                NewStatus = PersonnelChangeStatus.PendingHRReview,
                ActorAccountId = request.RequestedByAccountId,
                Note = "Demo tao ho so bien dong o buoc dau.",
                CreatedAt = request.CreatedAt
            });

            if (status != PersonnelChangeStatus.PendingHRReview)
            {
                context.PersonnelChangeHistories.Add(new PersonnelChangeHistory
                {
                    RequestId = request.Id,
                    Action = "StatusChanged",
                    OldStatus = PersonnelChangeStatus.PendingHRReview,
                    NewStatus = status,
                    ActorAccountId = demo.HrAccount.Id,
                    Note = "Demo chuyen sang buoc dau can xu ly tiep.",
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                });
            }

            if (status == PersonnelChangeStatus.PendingDirectorApproval || hasDirectorDecision)
            {
                context.PersonnelChangeApprovals.Add(new PersonnelChangeApproval
                {
                    RequestId = request.Id,
                    StepName = "DirectorApproval",
                    ApproverRole = "Director",
                    ApproverAccountId = demo.DirectorAccount.Id,
                    Decision = status == PersonnelChangeStatus.PendingDirectorApproval
                        ? PersonnelChangeApprovalDecision.Pending
                        : PersonnelChangeApprovalDecision.Approved,
                    Note = "Demo buoc duyet bien dong.",
                    DecidedAt = status == PersonnelChangeStatus.PendingDirectorApproval ? null : DateTime.UtcNow.AddHours(-2)
                });
            }

            context.PersonnelChangeRiskSnapshots.Add(new PersonnelChangeRiskSnapshot
            {
                RequestId = request.Id,
                CreatedByAccountId = demo.HrAccount.Id,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    employee = new { demo.Employee.EmployeeCode, demo.Employee.FullName },
                    currentContract = new { salary = 18000000, status = "Active" },
                    latestPerformance = new { score = 87.4, rating = "A" },
                    penaltySummary = new { count = penaltyRecordId.HasValue ? 1 : 0 },
                    seniority = new { months = 8 },
                    latestPayroll = new { netSalary = 23360000 },
                    history = new[] { "Onboarding", "ContractSigned" }
                })
            });

            if (request.RequiresContractFlow && contractFlowStarted)
            {
                context.PersonnelChangeContractLinks.Add(new PersonnelChangeContractLink
                {
                    PersonnelChangeRequestId = request.Id,
                    ContractFlowType = request.ContractFlowType,
                    Status = request.ContractFlowStatus ?? "Pending",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    CompletedAt = request.ContractFlowStatus == "Accepted" ? DateTime.UtcNow.AddHours(-1) : null
                });
            }
        }
    }
}
