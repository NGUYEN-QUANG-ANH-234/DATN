using HRM.backend.src.HRM.Application.DTOs.Dashboard;
using HRM.backend.src.HRM.Application.Interfaces.Dashboard.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HrmTaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Application.UseCases.Dashboard
{
    public class DashboardUseCase : IDashboardUseCase
    {
        private readonly MyDbContext _db;

        private static readonly EmployeeStatus[] ActiveEmployeeStatuses =
        {
            EmployeeStatus.Probation,
            EmployeeStatus.Official,
            EmployeeStatus.OnMaternityLeave
        };

        private static readonly PersonnelChangeStatus[] ClosedPersonnelChangeStatuses =
        {
            PersonnelChangeStatus.Completed,
            PersonnelChangeStatus.Rejected,
            PersonnelChangeStatus.Cancelled
        };

        public DashboardUseCase(MyDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardResponseDto> GetDashboardAsync(int accountId, string role, int? month, int? year, CancellationToken ct)
        {
            var period = ResolvePeriod(month, year);
            var actor = await ResolveActorAsync(accountId, role, ct);
            var response = new DashboardResponseDto
            {
                Role = actor.Role,
                Scope = actor.ScopeLabel,
                Month = period.Month,
                Year = period.Year,
                GeneratedAt = DateTime.UtcNow
            };

            switch (actor.Role)
            {
                case "Admin":
                    await BuildCompanyDashboardAsync(response, actor, period, includeSystemHealth: true, ct);
                    AddAdminActions(response);
                    break;
                case "Director":
                    await BuildCompanyDashboardAsync(response, actor, period, includeSystemHealth: false, ct);
                    AddDirectorActions(response);
                    break;
                case "HR":
                    await BuildCompanyDashboardAsync(response, actor, period, includeSystemHealth: false, ct);
                    AddHrActions(response);
                    break;
                case "Manager":
                    await BuildManagerDashboardAsync(response, actor, period, ct);
                    AddManagerActions(response);
                    break;
                case "Candidate":
                    await BuildCandidateDashboardAsync(response, actor, period, ct);
                    AddCandidateActions(response);
                    break;
                case "Collaborator":
                    await BuildCollaboratorDashboardAsync(response, actor, period, ct);
                    AddCollaboratorActions(response);
                    break;
                case "Intern":
                    await BuildInternDashboardAsync(response, actor, period, ct);
                    AddEmployeeActions(response);
                    break;
                default:
                    await BuildEmployeeDashboardAsync(response, actor, period, ct);
                    AddEmployeeActions(response);
                    break;
            }

            response.Widgets = response.Widgets.OrderBy(w => w.Order).ToList();
            response.Sections = response.Sections.OrderBy(s => s.Order).ToList();
            return response;
        }

        public async Task<DashboardDrilldownDto> GetDrilldownAsync(int accountId, string role, string type, int? month, int? year, string? scope, CancellationToken ct)
        {
            var period = ResolvePeriod(month, year);
            var actor = await ResolveActorAsync(accountId, role, ct);
            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();

            return normalizedType switch
            {
                "payroll-slip" => await BuildPayrollSlipDrilldownAsync(actor, period, ct),
                "payroll-summary" => await BuildPayrollSummaryDrilldownAsync(actor, period, ct),
                "payroll-preflight" => await BuildPayrollPreflightDrilldownAsync(actor, period, ct),
                "approval-list" => await BuildApprovalListDrilldownAsync(actor, period, ct),
                "attendance-reconciliation" => await BuildAttendanceDrilldownAsync(actor, period, ct),
                "recruitment-pipeline" => await BuildRecruitmentDrilldownAsync(actor, period, ct),
                "personnel-change-impact" => await BuildPersonnelChangeDrilldownAsync(actor, period, ct),
                "contract-lifecycle" => await BuildContractLifecycleDrilldownAsync(actor, period, ct),
                "profile-completeness" => await BuildProfileCompletenessDrilldownAsync(actor, ct),
                "system-health" => await BuildSystemHealthDrilldownAsync(actor, ct),
                "audit-log" => await BuildAuditLogDrilldownAsync(actor, ct),
                _ => new DashboardDrilldownDto
                {
                    Type = normalizedType,
                    Scope = actor.ScopeLabel,
                    Title = "Chi tiết dashboard",
                    Table = new DashboardTableDto
                    {
                        Columns = new List<string> { "Nội dung" },
                        Rows = new List<Dictionary<string, string?>>
                        {
                            new() { ["Nội dung"] = "Chưa hỗ trợ loại chi tiết này." }
                        }
                    }
                }
            };
        }

        private async Task BuildCompanyDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, bool includeSystemHealth, CancellationToken ct)
        {
            var start = period.Start;
            var end = period.End;
            var activeHeadcount = await ScopedEmployees(actor)
                .Where(e => ActiveEmployeeStatuses.Contains(e.Status))
                .CountAsync(ct);

            var attendanceToday = DateTime.UtcNow.Date;
            var attendanceCount = await ScopedAttendance(actor)
                .Where(a => a.WorkDate.Date == attendanceToday)
                .CountAsync(ct);

            var presentToday = await ScopedAttendance(actor)
                .Where(a => a.WorkDate.Date == attendanceToday && a.AttendanceStatus != AttendanceDailyStatus.Absence)
                .CountAsync(ct);

            var payrollQuery = ScopedPayrolls(actor).Where(p => p.Month == period.Month && p.Year == period.Year);
            var payrollCount = await payrollQuery.CountAsync(ct);
            var payrollCost = await payrollQuery.SumAsync(p => p.TotalCompanyCost ?? 0m, ct);
            var employerContribution = await payrollQuery.SumAsync(p => p.EmployerContributionAmount ?? 0m, ct);

            var openRecruitment = await _db.RecruitmentRequests.AsNoTracking()
                .Where(r => r.Status == RecruitmentRequestStatus.Approved && (!r.Deadline.HasValue || r.Deadline.Value >= DateTime.UtcNow.Date))
                .CountAsync(ct);

            var candidateInMonth = await _db.Candidates.AsNoTracking()
                .Where(c => c.AppliedDate >= start && c.AppliedDate < end)
                .CountAsync(ct);

            var pendingApprovals = await CountPendingApprovalsAsync(actor, period, ct);
            var openPersonnelChanges = await ScopedPersonnelChanges(actor)
                .Where(p => !ClosedPersonnelChangeStatuses.Contains(p.Status))
                .CountAsync(ct);

            var profileRate = await CalculateProfileCompletenessAsync(actor, ct);
            var activeContracts = await ScopedContracts(actor)
                .Where(c => c.Status == ContractStatus.Active)
                .CountAsync(ct);

            response.Widgets.AddRange(new[]
            {
                Widget("headcount", "Nhân sự đang làm việc", activeHeadcount.ToString("N0"), "Tính theo phạm vi được phép xem", "neutral", actor.ScopeLabel, 10, "profile-completeness"),
                Widget("attendance-today", "Chấm công hôm nay", Percent(presentToday, attendanceCount), $"{presentToday:N0}/{attendanceCount:N0} bản ghi có mặt", attendanceCount == 0 ? "warning" : "success", actor.ScopeLabel, 20, "attendance-reconciliation"),
                Widget("recruitment", "Tuyển dụng đang mở", openRecruitment.ToString("N0"), $"{candidateInMonth:N0} ứng viên mới trong tháng", "info", actor.ScopeLabel, 30, "recruitment-pipeline"),
                Widget("payroll-cost", "Chi phí lương tháng này", Money(payrollCost), $"{payrollCount:N0} phiếu lương, DN đóng {Money(employerContribution)}", payrollCount == 0 ? "warning" : "success", actor.ScopeLabel, 40, "payroll-summary"),
                Widget("approvals", "Việc cần phê duyệt", pendingApprovals.ToString("N0"), "Các yêu cầu đang chờ xử lý theo vai trò", pendingApprovals > 0 ? "warning" : "success", actor.ScopeLabel, 50, "approval-list"),
                Widget("personnel-changes", "Biến động nhân sự", openPersonnelChanges.ToString("N0"), "Hồ sơ chưa hoàn tất", openPersonnelChanges > 0 ? "info" : "success", actor.ScopeLabel, 60, "personnel-change-impact"),
                Widget("contracts", "Hợp đồng đang hiệu lực", activeContracts.ToString("N0"), $"Độ đầy đủ hồ sơ trung bình {profileRate:0.#}%", "neutral", actor.ScopeLabel, 70, "contract-lifecycle"),
            });

            response.Sections.Add(await BuildRecruitmentSectionAsync(actor, period, ct));
            response.Sections.Add(await BuildContractSectionAsync(actor, period, ct));
            response.Sections.Add(await BuildRiskSectionAsync(actor, period, ct));

            if (includeSystemHealth)
            {
                var violatedSla = await _db.SlaTrackingTasks.AsNoTracking()
                    .Where(s => s.Status == SlaTaskStatus.Violated)
                    .CountAsync(ct);
                var auditCount = await _db.AuditLogs.AsNoTracking()
                    .Where(a => a.Timestamp >= start && a.Timestamp < end)
                    .CountAsync(ct);

                response.Widgets.Add(Widget("system-health", "Theo dõi hệ thống", violatedSla.ToString("N0"), $"{auditCount:N0} audit log trong tháng", violatedSla > 0 ? "danger" : "success", "Toàn hệ thống", 80, "system-health"));
            }
        }

        private async Task BuildManagerDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var teamHeadcount = await ScopedEmployees(actor)
                .Where(e => ActiveEmployeeStatuses.Contains(e.Status))
                .CountAsync(ct);

            var attendanceToday = DateTime.UtcNow.Date;
            var attendance = await ScopedAttendance(actor)
                .Where(a => a.WorkDate.Date == attendanceToday)
                .ToListAsync(ct);

            var periodKeys = PeriodKeys(period);
            var kpiScores = await ScopedPerformanceReviews(actor)
                .Where(p => periodKeys.Contains(p.Period))
                .Select(p => (decimal?)p.TotalScore)
                .ToListAsync(ct);
            var avgKpi = kpiScores.Count == 0 ? 0m : kpiScores.Average() ?? 0m;

            var pendingApprovals = await CountPendingApprovalsAsync(actor, period, ct);
            var departmentPayroll = await ScopedPayrolls(actor)
                .Where(p => p.Month == period.Month && p.Year == period.Year)
                .SumAsync(p => p.TotalCompanyCost ?? 0m, ct);

            response.Widgets.AddRange(new[]
            {
                Widget("team-headcount", "Nhân sự trong phạm vi quản lý", teamHeadcount.ToString("N0"), actor.ScopeLabel, "neutral", actor.ScopeLabel, 10, "profile-completeness"),
                Widget("team-attendance", "Chấm công hôm nay", Percent(attendance.Count(a => a.AttendanceStatus != AttendanceDailyStatus.Absence), attendance.Count), "Tổng hợp từ bản ghi ngày hiện tại", attendance.Count == 0 ? "warning" : "success", actor.ScopeLabel, 20, "attendance-reconciliation"),
                Widget("team-kpi", "Điểm KPI trung bình", avgKpi.ToString("0.##"), $"Kỳ {period.Label}", avgKpi >= 80 ? "success" : "warning", actor.ScopeLabel, 30, "personnel-change-impact"),
                Widget("team-payroll", "Chi phí lương phòng ban", Money(departmentPayroll), "Chỉ hiển thị tổng hợp theo phòng ban", "info", actor.ScopeLabel, 40, "payroll-summary"),
                Widget("manager-approvals", "Việc cần duyệt", pendingApprovals.ToString("N0"), "OT, nghỉ phép, hợp đồng và biến động", pendingApprovals > 0 ? "warning" : "success", actor.ScopeLabel, 50, "approval-list"),
            });

            response.Sections.Add(await BuildTeamTaskSectionAsync(actor, period, ct));
            response.Sections.Add(await BuildRiskSectionAsync(actor, period, ct));
        }

        private async Task BuildEmployeeDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var employeeId = actor.EmployeeId;
            if (!employeeId.HasValue)
            {
                response.Widgets.Add(Widget("profile-missing", "Chưa có hồ sơ nhân sự", "0", "Tài khoản chưa liên kết với hồ sơ nhân sự.", "warning", actor.ScopeLabel, 10, "profile-completeness"));
                return;
            }

            var payroll = await _db.Payrolls.AsNoTracking()
                .Where(p => p.EmployeeId == employeeId && p.Month == period.Month && p.Year == period.Year)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct);

            var today = DateTime.UtcNow.Date;
            var attendance = await _db.AttendanceDailySummaries.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && a.WorkDate.Date == today)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(ct);

            var periodKeys = PeriodKeys(period);
            var kpi = await _db.PerformanceReviews.AsNoTracking()
                .Where(p => p.EmployeeId == employeeId && periodKeys.Contains(p.Period))
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct);

            var activeContract = await _db.Contracts.AsNoTracking()
                .Where(c => c.EmployeeId == employeeId && c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync(ct);

            var leavePending = await _db.LeaveRequests.AsNoTracking()
                .Where(l => l.EmployeeId == employeeId && (l.Status == LeaveRequestStatus.Pending || l.Status == LeaveRequestStatus.PendingDept || l.Status == LeaveRequestStatus.PendingDirector))
                .CountAsync(ct);

            var otPending = await _db.OvertimeRequests.AsNoTracking()
                .Where(o => o.EmployeeId == employeeId && (o.Status == OvertimeRequestStatus.PendingManager || o.Status == OvertimeRequestStatus.PendingHR || o.Status == OvertimeRequestStatus.PendingDirector))
                .CountAsync(ct);

            var completeness = await CalculateProfileCompletenessAsync(actor, ct);

            response.Widgets.AddRange(new[]
            {
                Widget("my-payroll", "Lương tháng này", payroll == null ? "Chưa có" : Money(payroll.NetSalary ?? 0m), payroll == null ? $"Chưa phát sinh phiếu lương {period.Label}" : $"Trạng thái: {payroll.Status}", payroll == null ? "warning" : "success", "Cá nhân", 10, "payroll-slip"),
                Widget("my-attendance", "Chấm công hôm nay", attendance == null ? "Chưa có" : AttendanceLabel(attendance.AttendanceStatus), attendance == null ? "Chưa có bản ghi trong ngày" : $"{attendance.WorkingMinutes / 60m:0.##} giờ làm việc", attendance == null ? "warning" : "success", "Cá nhân", 20, "attendance-reconciliation"),
                Widget("my-requests", "Đơn đang chờ", (leavePending + otPending).ToString("N0"), $"{leavePending:N0} đơn nghỉ, {otPending:N0} đơn OT", leavePending + otPending > 0 ? "info" : "success", "Cá nhân", 30, "approval-list"),
                Widget("my-kpi", "KPI kỳ này", kpi == null ? "Chưa có" : kpi.TotalScore.ToString("0.##"), kpi == null ? $"Chưa có phiếu KPI {period.Label}" : $"Trạng thái: {kpi.Status}", kpi != null && kpi.TotalScore >= 80 ? "success" : "neutral", "Cá nhân", 40, "personnel-change-impact"),
                Widget("my-contract", "Hợp đồng hiện tại", activeContract == null ? "Chưa có" : activeContract.ContractNumber, activeContract == null ? "Chưa có hợp đồng active" : $"{activeContract.ContractType} từ {Date(activeContract.StartDate)}", activeContract == null ? "warning" : "success", "Cá nhân", 50, "contract-lifecycle"),
                Widget("my-profile", "Độ đầy đủ hồ sơ", $"{completeness:0.#}%", "Thông tin định danh, liên hệ, ngân hàng", completeness >= 80 ? "success" : "warning", "Cá nhân", 60, "profile-completeness")
            });

            response.Sections.Add(await BuildPersonalWorkSectionAsync(actor, period, ct));
        }

        private async Task BuildInternDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            await BuildEmployeeDashboardAsync(response, actor, period, ct);

            if (!actor.EmployeeId.HasValue) return;

            var trainings = await _db.Trainings.AsNoTracking()
                .Where(t => t.EmployeeId == actor.EmployeeId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync(ct);

            var current = trainings.FirstOrDefault(t => t.Status == TrainingStatus.InProgress || t.Status == TrainingStatus.PendingEvaluation || t.Status == TrainingStatus.Extended);
            response.Widgets.Add(Widget("intern-training", "Tiến độ thực tập", current == null ? "Chưa có" : TrainingStatusLabel(current.Status), current?.CourseName ?? "Chưa có chương trình đang theo dõi", current == null ? "neutral" : "info", "Cá nhân", 70, "personnel-change-impact"));

            response.Sections.Add(new DashboardSectionDto
            {
                Id = "training",
                Title = "Đào tạo và nhiệm vụ thực tập",
                Subtitle = "Các chương trình gần nhất của bạn",
                Order = 30,
                Table = Table(new[] { "Chương trình", "Trạng thái", "Điểm", "Hạn đánh giá" },
                    trainings.Select(t => Row(
                        ("Chương trình", t.CourseName ?? $"Đào tạo #{t.Id}"),
                        ("Trạng thái", TrainingStatusLabel(t.Status)),
                        ("Điểm", t.FinalScore?.ToString("0.##") ?? "-"),
                        ("Hạn đánh giá", Date(t.EvaluationDeadline))
                    )))
            });
        }

        private async Task BuildCollaboratorDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            await BuildEmployeeDashboardAsync(response, actor, period, ct);

            if (!actor.EmployeeId.HasValue) return;

            var lines = await _db.ExternalTimesheetLines.AsNoTracking()
                .Include(l => l.Import)
                .Where(l => l.CollaboratorEmployeeId == actor.EmployeeId && l.WorkDate >= period.Start && l.WorkDate < period.End)
                .ToListAsync(ct);

            response.Widgets.Add(Widget("collaborator-hours", "Giờ công đã duyệt", lines.Sum(l => l.ApprovedHours).ToString("0.##"), $"Giá trị tạm tính {Money(lines.Sum(l => l.Amount))}", lines.Count == 0 ? "warning" : "success", "Cá nhân", 70, "payroll-slip"));
            response.Sections.Add(new DashboardSectionDto
            {
                Id = "external-timesheet",
                Title = "Giờ công cộng tác viên",
                Subtitle = $"Dữ liệu trong kỳ {period.Label}",
                Order = 30,
                Table = Table(new[] { "Ngày", "Dự án", "Giờ duyệt", "Thành tiền", "Trạng thái" },
                    lines.OrderByDescending(l => l.WorkDate).Take(10).Select(l => Row(
                        ("Ngày", Date(l.WorkDate)),
                        ("Dự án", l.ProjectCode ?? "-"),
                        ("Giờ duyệt", l.ApprovedHours.ToString("0.##")),
                        ("Thành tiền", Money(l.Amount)),
                        ("Trạng thái", l.Import.Status.ToString())
                    )))
            });
        }

        private async Task BuildCandidateDashboardAsync(DashboardResponseDto response, DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var applications = await _db.Candidates.AsNoTracking()
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r!.Department)
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r!.Position)
                .Where(c => c.Email != null && c.Email == actor.Email)
                .OrderByDescending(c => c.AppliedDate)
                .ToListAsync(ct);

            var activeApplications = applications.Count(c => c.Status != CandidateStatus.Hired && c.Status != CandidateStatus.Rejected && c.Status != CandidateStatus.SLA_Expired);
            var latest = applications.FirstOrDefault();
            var openJobs = await _db.RecruitmentRequests.AsNoTracking()
                .Where(r => r.Status == RecruitmentRequestStatus.Approved && (!r.Deadline.HasValue || r.Deadline.Value >= DateTime.UtcNow.Date))
                .CountAsync(ct);

            response.Widgets.AddRange(new[]
            {
                Widget("candidate-applications", "Hồ sơ ứng tuyển", applications.Count.ToString("N0"), $"{activeApplications:N0} hồ sơ đang xử lý", activeApplications > 0 ? "info" : "neutral", "Cá nhân", 10, "recruitment-pipeline"),
                Widget("candidate-status", "Trạng thái gần nhất", latest == null ? "Chưa ứng tuyển" : CandidateStatusLabel(latest.Status), latest?.RecruitmentRequest?.Position?.Title ?? "Bạn có thể xem các vị trí đang tuyển", latest == null ? "neutral" : "success", "Cá nhân", 20, "recruitment-pipeline"),
                Widget("candidate-open-jobs", "Vị trí đang tuyển", openJobs.ToString("N0"), "Các vị trí còn nhận hồ sơ", "info", "Công khai", 30, "recruitment-pipeline")
            });

            response.Sections.Add(new DashboardSectionDto
            {
                Id = "applications",
                Title = "Hồ sơ ứng tuyển của tôi",
                Subtitle = "Theo dõi từng vị trí bạn đã nộp",
                Order = 10,
                Table = Table(new[] { "Vị trí", "Phòng ban", "Ngày nộp", "Trạng thái", "Mã theo dõi" },
                    applications.Select(c => Row(
                        ("Vị trí", c.RecruitmentRequest?.Position?.Title ?? "-"),
                        ("Phòng ban", c.RecruitmentRequest?.Department?.DeptName ?? "-"),
                        ("Ngày nộp", Date(c.AppliedDate)),
                        ("Trạng thái", CandidateStatusLabel(c.Status)),
                        ("Mã theo dõi", c.TrackingCode ?? "-")
                    )))
            });
        }

        private async Task<DashboardSectionDto> BuildRecruitmentSectionAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var requests = await _db.RecruitmentRequests.AsNoTracking()
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Include(r => r.Candidates)
                .Where(r => r.Status == RecruitmentRequestStatus.Approved || r.Status == RecruitmentRequestStatus.PendingDirector || r.Status == RecruitmentRequestStatus.PendingHR)
                .OrderBy(r => r.Deadline ?? DateTime.MaxValue)
                .Take(8)
                .ToListAsync(ct);

            return new DashboardSectionDto
            {
                Id = "recruitment",
                Title = "Tuyển dụng cần theo dõi",
                Subtitle = "Nhu cầu tuyển dụng và số ứng viên hiện có",
                Order = 10,
                Table = Table(new[] { "Vị trí", "Phòng ban", "Chỉ tiêu", "Ứng viên", "Hạn", "Trạng thái" },
                    requests.Select(r => Row(
                        ("Vị trí", r.Position?.Title ?? "-"),
                        ("Phòng ban", r.Department?.DeptName ?? "-"),
                        ("Chỉ tiêu", r.Quantity.ToString("N0")),
                        ("Ứng viên", r.Candidates.Count.ToString("N0")),
                        ("Hạn", Date(r.Deadline)),
                        ("Trạng thái", RecruitmentStatusLabel(r.Status))
                    )))
            };
        }

        private async Task<DashboardSectionDto> BuildContractSectionAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var contracts = await ScopedContracts(actor)
                .Include(c => c.Employee)
                .ThenInclude(e => e!.Department)
                .OrderByDescending(c => c.StartDate)
                .Take(8)
                .ToListAsync(ct);

            return new DashboardSectionDto
            {
                Id = "contracts",
                Title = "Hợp đồng gần đây",
                Subtitle = "Theo dõi hiệu lực và các hồ sơ đang xử lý",
                Order = 20,
                Table = Table(new[] { "Nhân sự", "Phòng ban", "Số hợp đồng", "Loại", "Hiệu lực", "Trạng thái" },
                    contracts.Select(c => Row(
                        ("Nhân sự", c.Employee?.FullName ?? "-"),
                        ("Phòng ban", c.Employee?.Department?.DeptName ?? "-"),
                        ("Số hợp đồng", c.ContractNumber),
                        ("Loại", c.ContractType.ToString()),
                        ("Hiệu lực", $"{Date(c.StartDate)} - {Date(c.EndDate)}"),
                        ("Trạng thái", ContractStatusLabel(c.Status))
                    )))
            };
        }

        private async Task<DashboardSectionDto> BuildRiskSectionAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var overdueSla = await _db.SlaTrackingTasks.AsNoTracking()
                .Where(s => s.Status == SlaTaskStatus.Violated || (s.ResolvedAt == null && s.Deadline < DateTime.UtcNow))
                .OrderBy(s => s.Deadline)
                .Take(5)
                .ToListAsync(ct);

            return new DashboardSectionDto
            {
                Id = "risks",
                Title = "Cần chú ý",
                Subtitle = "Các hạn xử lý và hồ sơ có rủi ro vận hành",
                Order = 90,
                Table = Table(new[] { "Nguồn", "Mã hồ sơ", "Hạn xử lý", "Trạng thái" },
                    overdueSla.Select(s => Row(
                        ("Nguồn", s.ModuleType.ToString()),
                        ("Mã hồ sơ", $"#{s.ReferenceId}"),
                        ("Hạn xử lý", Date(s.Deadline)),
                        ("Trạng thái", s.Status.ToString())
                    )))
            };
        }

        private async Task<DashboardSectionDto> BuildTeamTaskSectionAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var tasks = await _db.Tasks.AsNoTracking()
                .Include(t => t.Assignee)
                .Where(t => t.AssignedTo != null && ScopedEmployeeIds(actor).Contains(t.AssignedTo.Value))
                .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                .Take(10)
                .ToListAsync(ct);

            return new DashboardSectionDto
            {
                Id = "team-tasks",
                Title = "Công việc trong nhóm",
                Subtitle = "Các nhiệm vụ đang mở hoặc chờ đánh giá",
                Order = 20,
                Table = Table(new[] { "Công việc", "Nhân sự", "Tiến độ", "Hạn", "Trạng thái" },
                    tasks.Select(t => Row(
                        ("Công việc", t.Title),
                        ("Nhân sự", t.Assignee?.FullName ?? "-"),
                        ("Tiến độ", $"{t.ProgressPercent}%"),
                        ("Hạn", Date(t.Deadline)),
                        ("Trạng thái", TaskStatusLabel(t.Status))
                    )))
            };
        }

        private async Task<DashboardSectionDto> BuildPersonalWorkSectionAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var employeeId = actor.EmployeeId ?? 0;
            var tasks = await _db.Tasks.AsNoTracking()
                .Where(t => t.AssignedTo == employeeId)
                .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                .Take(8)
                .ToListAsync(ct);

            return new DashboardSectionDto
            {
                Id = "my-work",
                Title = "Việc của tôi",
                Subtitle = "Công việc, deadline và trạng thái gần nhất",
                Order = 20,
                Table = Table(new[] { "Công việc", "Tiến độ", "Hạn", "Trạng thái" },
                    tasks.Select(t => Row(
                        ("Công việc", t.Title),
                        ("Tiến độ", $"{t.ProgressPercent}%"),
                        ("Hạn", Date(t.Deadline)),
                        ("Trạng thái", TaskStatusLabel(t.Status))
                    )))
            };
        }

        private async Task<DashboardDrilldownDto> BuildPayrollSlipDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var payrolls = await ScopedPayrolls(actor)
                .Where(p => p.Month == period.Month && p.Year == period.Year)
                .Include(p => p.Employee)
                .OrderByDescending(p => p.Id)
                .Take(actor.IsCompanyScope ? 50 : 12)
                .ToListAsync(ct);

            var showCompanyCost = actor.IsCompanyScope || actor.Role == "Manager";
            var columns = showCompanyCost
                ? new[] { "Nhân sự", "Kỳ", "Lương thực nhận", "Chi phí công ty", "Trạng thái" }
                : new[] { "Kỳ", "Lương thực nhận", "Ngày công", "OT", "Trạng thái" };

            var rows = showCompanyCost
                ? payrolls.Select(p => Row(
                    ("Nhân sự", p.Employee?.FullName ?? "-"),
                    ("Kỳ", PeriodLabel(p.Month, p.Year)),
                    ("Lương thực nhận", actor.IsCompanyScope ? Money(p.NetSalary ?? 0m) : "Ẩn chi tiết"),
                    ("Chi phí công ty", Money(p.TotalCompanyCost ?? 0m)),
                    ("Trạng thái", p.Status.ToString())))
                : payrolls.Select(p => Row(
                    ("Kỳ", PeriodLabel(p.Month, p.Year)),
                    ("Lương thực nhận", Money(p.NetSalary ?? 0m)),
                    ("Ngày công", p.ActualWorkDays?.ToString("0.##") ?? "-"),
                    ("OT", p.ActualOtMinutes.HasValue ? $"{p.ActualOtMinutes.Value / 60m:0.##} giờ" : "-"),
                    ("Trạng thái", p.Status.ToString())));

            return Drilldown("payroll-slip", actor.ScopeLabel, "Chi tiết phiếu lương", columns, rows);
        }

        private async Task<DashboardDrilldownDto> BuildPayrollSummaryDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var payrolls = await ScopedPayrolls(actor)
                .Where(p => p.Month == period.Month && p.Year == period.Year)
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.Department)
                .ToListAsync(ct);

            var result = Drilldown(
                "payroll-summary",
                actor.ScopeLabel,
                $"Tổng hợp lương {period.Label}",
                actor.IsCompanyScope
                    ? new[] { "Nhân sự", "Phòng ban", "Gross", "DN đóng", "Tổng chi phí", "Trạng thái" }
                    : new[] { "Phòng ban", "Số phiếu", "Tổng chi phí", "Trạng thái" },
                actor.IsCompanyScope
                    ? payrolls.Take(80).Select(p => Row(
                        ("Nhân sự", p.Employee?.FullName ?? "-"),
                        ("Phòng ban", p.Employee?.Department?.DeptName ?? "-"),
                        ("Gross", Money(p.GrossIncome ?? p.GrossSalary ?? 0m)),
                        ("DN đóng", Money(p.EmployerContributionAmount ?? 0m)),
                        ("Tổng chi phí", Money(p.TotalCompanyCost ?? 0m)),
                        ("Trạng thái", p.Status.ToString())))
                    : payrolls.GroupBy(p => p.Employee?.Department?.DeptName ?? actor.ScopeLabel).Select(g => Row(
                        ("Phòng ban", g.Key),
                        ("Số phiếu", g.Count().ToString("N0")),
                        ("Tổng chi phí", Money(g.Sum(p => p.TotalCompanyCost ?? 0m))),
                        ("Trạng thái", string.Join(", ", g.Select(p => p.Status.ToString()).Distinct().Take(3)))))
            );

            result.Metrics.Add(new DashboardMetricDto { Label = "Tổng chi phí", Value = Money(payrolls.Sum(p => p.TotalCompanyCost ?? 0m)), Severity = "info" });
            result.Metrics.Add(new DashboardMetricDto { Label = "Doanh nghiệp đóng", Value = Money(payrolls.Sum(p => p.EmployerContributionAmount ?? 0m)), Severity = "neutral" });
            result.Metrics.Add(new DashboardMetricDto { Label = "Số phiếu", Value = payrolls.Count.ToString("N0"), NumericValue = payrolls.Count, Severity = "neutral" });
            return result;
        }

        private async Task<DashboardDrilldownDto> BuildPayrollPreflightDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            if (!actor.IsCompanyScope)
            {
                return ForbiddenDrilldown("payroll-preflight", actor.ScopeLabel);
            }

            var payrollPolicies = await _db.PayrollPolicies.AsNoTracking().CountAsync(ct);
            var activePayrollPolicies = await _db.PayrollPolicies.AsNoTracking().CountAsync(p => p.Status == PolicyVersionStatus.Active, ct);
            var taxConfigs = await _db.TaxConfigs.AsNoTracking().CountAsync(ct);
            var insuranceConfigs = await _db.InsuranceConfigs.AsNoTracking().CountAsync(ct);
            var otConfigs = await _db.OvertimeRateConfigs.AsNoTracking().CountAsync(ct);
            var calendars = await _db.CompanyCalendars.AsNoTracking().CountAsync(c => c.Year == period.Year, ct);

            return Drilldown("payroll-preflight", "Toàn hệ thống", $"Cấu hình tính lương {period.Label}", new[] { "Nhóm cấu hình", "Số lượng", "Ghi chú" }, new[]
            {
                Row(("Nhóm cấu hình", "Policy lương"), ("Số lượng", $"{activePayrollPolicies:N0}/{payrollPolicies:N0} active"), ("Ghi chú", "Thuế, bảo hiểm, OT, phụ cấp")),
                Row(("Nhóm cấu hình", "Thuế TNCN"), ("Số lượng", taxConfigs.ToString("N0")), ("Ghi chú", "Theo version hiệu lực")),
                Row(("Nhóm cấu hình", "Bảo hiểm"), ("Số lượng", insuranceConfigs.ToString("N0")), ("Ghi chú", "Theo version hiệu lực")),
                Row(("Nhóm cấu hình", "OT"), ("Số lượng", otConfigs.ToString("N0")), ("Ghi chú", "Theo loại ngày/ca")),
                Row(("Nhóm cấu hình", "Lịch công ty"), ("Số lượng", calendars.ToString("N0")), ("Ghi chú", $"Năm {period.Year}"))
            });
        }

        private async Task<DashboardDrilldownDto> BuildApprovalListDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var rows = new List<Dictionary<string, string?>>();

            if (actor.IsCompanyScope || actor.Role == "Manager")
            {
                var recruitment = await _db.RecruitmentRequests.AsNoTracking()
                    .Include(r => r.Department)
                    .Include(r => r.Position)
                    .Where(r => r.Status == RecruitmentRequestStatus.PendingHR || r.Status == RecruitmentRequestStatus.PendingDirector)
                    .OrderBy(r => r.Deadline ?? DateTime.MaxValue)
                    .Take(15)
                    .ToListAsync(ct);

                rows.AddRange(recruitment.Select(r => Row(
                    ("Phân hệ", "Tuyển dụng"),
                    ("Hồ sơ", r.Position?.Title ?? $"Nhu cầu #{r.Id}"),
                    ("Người liên quan", r.Department?.DeptName ?? "-"),
                    ("Hạn/SLA", Date(r.Deadline)),
                    ("Trạng thái", RecruitmentStatusLabel(r.Status)))));
            }

            var pendingContracts = await ScopedContracts(actor)
                .Include(c => c.Employee)
                .Where(c => c.Status == ContractStatus.PendingDept || c.Status == ContractStatus.PendingManagerContentReview || c.Status == ContractStatus.PendingHR || c.Status == ContractStatus.PendingDirector || c.Status == ContractStatus.PendingEmployee || c.Status == ContractStatus.PendingHRRevision)
                .OrderByDescending(c => c.StartDate)
                .Take(15)
                .ToListAsync(ct);

            rows.AddRange(pendingContracts.Select(c => Row(
                ("Phân hệ", "Hợp đồng"),
                ("Hồ sơ", c.ContractNumber),
                ("Người liên quan", c.Employee?.FullName ?? "-"),
                ("Hạn/SLA", Date(c.DirectorDeadline ?? c.EmployeeDeadline)),
                ("Trạng thái", ContractStatusLabel(c.Status)))));

            var personnelChanges = await ScopedPersonnelChanges(actor)
                .Include(p => p.Employee)
                .Where(p => !ClosedPersonnelChangeStatuses.Contains(p.Status))
                .OrderBy(p => p.EffectiveDate ?? p.RequestedAt)
                .Take(15)
                .ToListAsync(ct);

            rows.AddRange(personnelChanges.Select(p => Row(
                ("Phân hệ", "Biến động nhân sự"),
                ("Hồ sơ", PersonnelChangeTypeLabel(p.ChangeType)),
                ("Người liên quan", p.Employee?.FullName ?? "-"),
                ("Hạn/SLA", Date(p.EffectiveDate)),
                ("Trạng thái", PersonnelChangeStatusLabel(p.Status)))));

            return new DashboardDrilldownDto
            {
                Type = "approval-list",
                Scope = actor.ScopeLabel,
                Title = "Việc cần xử lý",
                Table = Table(new[] { "Phân hệ", "Hồ sơ", "Người liên quan", "Hạn/SLA", "Trạng thái" }, rows)
            };
        }

        private async Task<DashboardDrilldownDto> BuildAttendanceDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var rows = await ScopedAttendance(actor)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Where(a => a.WorkDate >= period.Start && a.WorkDate < period.End)
                .OrderByDescending(a => a.WorkDate)
                .Take(80)
                .ToListAsync(ct);

            var result = Drilldown("attendance-reconciliation", actor.ScopeLabel, $"Chấm công {period.Label}", new[] { "Ngày", "Nhân sự", "Phòng ban", "Công", "Đi muộn", "OT", "Trạng thái" },
                rows.Select(a => Row(
                    ("Ngày", Date(a.WorkDate)),
                    ("Nhân sự", a.Employee.FullName),
                    ("Phòng ban", a.Employee.Department?.DeptName ?? "-"),
                    ("Công", a.WorkdayValue.ToString("0.##")),
                    ("Đi muộn", $"{a.LateMinutes} phút"),
                    ("OT", $"{a.OvertimeMinutes} phút"),
                    ("Trạng thái", AttendanceLabel(a.AttendanceStatus))
                )));

            result.Metrics.Add(new DashboardMetricDto { Label = "Ngày công", Value = rows.Sum(r => r.WorkdayValue).ToString("0.##"), NumericValue = rows.Sum(r => r.WorkdayValue) });
            result.Metrics.Add(new DashboardMetricDto { Label = "Đi muộn", Value = $"{rows.Sum(r => r.LateMinutes):N0} phút", NumericValue = rows.Sum(r => r.LateMinutes), Severity = rows.Sum(r => r.LateMinutes) > 0 ? "warning" : "success" });
            return result;
        }

        private async Task<DashboardDrilldownDto> BuildRecruitmentDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            if (actor.Role == "Candidate")
            {
                return await BuildCandidateRecruitmentDrilldownAsync(actor, ct);
            }

            var candidates = await _db.Candidates.AsNoTracking()
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r!.Position)
                .Where(c => c.AppliedDate >= period.Start && c.AppliedDate < period.End)
                .OrderByDescending(c => c.AppliedDate)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("recruitment-pipeline", actor.ScopeLabel, $"Pipeline tuyển dụng {period.Label}", new[] { "Ứng viên", "Vị trí", "Ngày nộp", "Mã theo dõi", "Trạng thái" },
                candidates.Select(c => Row(
                    ("Ứng viên", c.FullName),
                    ("Vị trí", c.RecruitmentRequest?.Position?.Title ?? "-"),
                    ("Ngày nộp", Date(c.AppliedDate)),
                    ("Mã theo dõi", c.TrackingCode ?? "-"),
                    ("Trạng thái", CandidateStatusLabel(c.Status))
                )));
        }

        private async Task<DashboardDrilldownDto> BuildCandidateRecruitmentDrilldownAsync(DashboardActor actor, CancellationToken ct)
        {
            var applications = await _db.Candidates.AsNoTracking()
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r!.Department)
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r!.Position)
                .Where(c => c.Email != null && c.Email == actor.Email)
                .OrderByDescending(c => c.AppliedDate)
                .ToListAsync(ct);

            return Drilldown("recruitment-pipeline", "Cá nhân", "Theo dõi ứng tuyển", new[] { "Vị trí", "Phòng ban", "Ngày nộp", "Mã theo dõi", "Trạng thái" },
                applications.Select(c => Row(
                    ("Vị trí", c.RecruitmentRequest?.Position?.Title ?? "-"),
                    ("Phòng ban", c.RecruitmentRequest?.Department?.DeptName ?? "-"),
                    ("Ngày nộp", Date(c.AppliedDate)),
                    ("Mã theo dõi", c.TrackingCode ?? "-"),
                    ("Trạng thái", CandidateStatusLabel(c.Status))
                )));
        }

        private async Task<DashboardDrilldownDto> BuildPersonnelChangeDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var items = await ScopedPersonnelChanges(actor)
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.Department)
                .OrderByDescending(p => p.RequestedAt)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("personnel-change-impact", actor.ScopeLabel, "Biến động nhân sự", new[] { "Nhân sự", "Phòng ban", "Nghiệp vụ", "Hiệu lực", "Trạng thái" },
                items.Select(p => Row(
                    ("Nhân sự", p.Employee?.FullName ?? "-"),
                    ("Phòng ban", p.Employee?.Department?.DeptName ?? "-"),
                    ("Nghiệp vụ", PersonnelChangeTypeLabel(p.ChangeType)),
                    ("Hiệu lực", Date(p.EffectiveDate)),
                    ("Trạng thái", PersonnelChangeStatusLabel(p.Status))
                )));
        }

        private async Task<DashboardDrilldownDto> BuildContractLifecycleDrilldownAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var contracts = await ScopedContracts(actor)
                .Include(c => c.Employee)
                    .ThenInclude(e => e!.Department)
                .OrderByDescending(c => c.StartDate)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("contract-lifecycle", actor.ScopeLabel, "Vòng đời hợp đồng", new[] { "Nhân sự", "Phòng ban", "Số hợp đồng", "Loại", "Hiệu lực", "Trạng thái" },
                contracts.Select(c => Row(
                    ("Nhân sự", c.Employee?.FullName ?? "-"),
                    ("Phòng ban", c.Employee?.Department?.DeptName ?? "-"),
                    ("Số hợp đồng", c.ContractNumber),
                    ("Loại", c.ContractType.ToString()),
                    ("Hiệu lực", $"{Date(c.StartDate)} - {Date(c.EndDate)}"),
                    ("Trạng thái", ContractStatusLabel(c.Status))
                )));
        }

        private async Task<DashboardDrilldownDto> BuildProfileCompletenessDrilldownAsync(DashboardActor actor, CancellationToken ct)
        {
            var employees = await ScopedEmployees(actor)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .OrderBy(e => e.FullName)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("profile-completeness", actor.ScopeLabel, "Độ đầy đủ hồ sơ", new[] { "Nhân sự", "Phòng ban", "Chức danh", "Mức đầy đủ", "Thiếu chính" },
                employees.Select(e =>
                {
                    var missing = MissingProfileFields(e);
                    return Row(
                        ("Nhân sự", e.FullName),
                        ("Phòng ban", e.Department?.DeptName ?? "-"),
                        ("Chức danh", e.Position?.Title ?? "-"),
                        ("Mức đầy đủ", $"{ProfileCompleteness(e):0.#}%"),
                        ("Thiếu chính", missing.Count == 0 ? "Đủ" : string.Join(", ", missing.Take(3)))
                    );
                }));
        }

        private async Task<DashboardDrilldownDto> BuildSystemHealthDrilldownAsync(DashboardActor actor, CancellationToken ct)
        {
            if (actor.Role != "Admin" && actor.Role != "Director")
            {
                return ForbiddenDrilldown("system-health", actor.ScopeLabel);
            }

            var sla = await _db.SlaTrackingTasks.AsNoTracking()
                .OrderBy(s => s.Deadline)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("system-health", "Toàn hệ thống", "Theo dõi SLA hệ thống", new[] { "Nguồn", "Mã hồ sơ", "Hạn xử lý", "Hoàn tất", "Trạng thái" },
                sla.Select(s => Row(
                    ("Nguồn", s.ModuleType.ToString()),
                    ("Mã hồ sơ", $"#{s.ReferenceId}"),
                    ("Hạn xử lý", Date(s.Deadline)),
                    ("Hoàn tất", Date(s.ResolvedAt)),
                    ("Trạng thái", s.Status.ToString())
                )));
        }

        private async Task<DashboardDrilldownDto> BuildAuditLogDrilldownAsync(DashboardActor actor, CancellationToken ct)
        {
            if (actor.Role != "Admin" && actor.Role != "Director")
            {
                return ForbiddenDrilldown("audit-log", actor.ScopeLabel);
            }

            var logs = await _db.AuditLogs.AsNoTracking()
                .Include(a => a.Account)
                .OrderByDescending(a => a.Timestamp)
                .Take(80)
                .ToListAsync(ct);

            return Drilldown("audit-log", "Toàn hệ thống", "Audit log gần nhất", new[] { "Thời điểm", "Tài khoản", "Bảng", "Hành động", "Trường thay đổi" },
                logs.Select(a => Row(
                    ("Thời điểm", DateTimeLabel(a.Timestamp)),
                    ("Tài khoản", a.Account?.Email ?? "-"),
                    ("Bảng", a.TableName ?? "-"),
                    ("Hành động", a.ActionType ?? "-"),
                    ("Trường thay đổi", a.AffectedColumns ?? "-")
                )));
        }

        private async Task<int> CountPendingApprovalsAsync(DashboardActor actor, DashboardPeriod period, CancellationToken ct)
        {
            var count = 0;

            if (actor.Role is "Admin" or "HR")
            {
                count += await _db.RecruitmentRequests.AsNoTracking().CountAsync(r => r.Status == RecruitmentRequestStatus.PendingHR, ct);
                count += await _db.Contracts.AsNoTracking().CountAsync(c => c.Status == ContractStatus.PendingHR || c.Status == ContractStatus.PendingHRRevision, ct);
                count += await _db.ContractAddendums.AsNoTracking().CountAsync(a => a.Status == AddendumStatus.PendingHR || a.Status == AddendumStatus.PendingHRRevision, ct);
                count += await _db.ProfileUpdateRequests.AsNoTracking().CountAsync(p => p.Status == RequestStatus.Pending_HR, ct);
                count += await _db.Payrolls.AsNoTracking().CountAsync(p => p.Month == period.Month && p.Year == period.Year && p.Status == PayrollStatus.Calculated, ct);
                count += await _db.PersonnelChangeRequests.AsNoTracking().CountAsync(p => p.Status == PersonnelChangeStatus.PendingHRReview || p.Status == PersonnelChangeStatus.PendingEmployeeNotification, ct);
            }

            if (actor.Role is "Admin" or "Director")
            {
                count += await _db.RecruitmentRequests.AsNoTracking().CountAsync(r => r.Status == RecruitmentRequestStatus.PendingDirector, ct);
                count += await _db.Contracts.AsNoTracking().CountAsync(c => c.Status == ContractStatus.PendingDirector, ct);
                count += await _db.ContractAddendums.AsNoTracking().CountAsync(a => a.Status == AddendumStatus.PendingDirector, ct);
                count += await _db.Payrolls.AsNoTracking().CountAsync(p => p.Month == period.Month && p.Year == period.Year && p.Status == PayrollStatus.PendingApproval, ct);
                count += await _db.PersonnelChangeRequests.AsNoTracking().CountAsync(p => p.Status == PersonnelChangeStatus.PendingDirectorApproval, ct);
            }

            if (actor.Role is "Admin" or "Manager")
            {
                var scopedIds = ScopedEmployeeIds(actor);
                count += await _db.OvertimeRequests.AsNoTracking().CountAsync(o => scopedIds.Contains(o.EmployeeId) && o.Status == OvertimeRequestStatus.PendingManager, ct);
                count += await _db.LeaveRequests.AsNoTracking().CountAsync(l => l.EmployeeId.HasValue && scopedIds.Contains(l.EmployeeId.Value) && (l.Status == LeaveRequestStatus.Pending || l.Status == LeaveRequestStatus.PendingDept), ct);
                count += await _db.Contracts.AsNoTracking().CountAsync(c => c.EmployeeId.HasValue && scopedIds.Contains(c.EmployeeId.Value) && (c.Status == ContractStatus.PendingDept || c.Status == ContractStatus.PendingManagerContentReview), ct);
                count += await _db.ContractAddendums.AsNoTracking().Include(a => a.Contract).CountAsync(a => a.Contract != null && a.Contract.EmployeeId.HasValue && scopedIds.Contains(a.Contract.EmployeeId.Value) && a.Status == AddendumStatus.PendingDept, ct);
                count += await _db.PersonnelChangeRequests.AsNoTracking().CountAsync(p => p.EmployeeId.HasValue && scopedIds.Contains(p.EmployeeId.Value) && (p.Status == PersonnelChangeStatus.PendingManagerReview || p.Status == PersonnelChangeStatus.PendingCurrentManagerOpinion), ct);
            }

            if (actor.Role is "Employee" or "Intern" or "Collaborator" && actor.EmployeeId.HasValue)
            {
                count += await _db.Contracts.AsNoTracking().CountAsync(c => c.EmployeeId == actor.EmployeeId && c.Status == ContractStatus.PendingEmployee, ct);
                count += await _db.ContractAddendums.AsNoTracking().Include(a => a.Contract).CountAsync(a => a.Contract != null && a.Contract.EmployeeId == actor.EmployeeId && a.Status == AddendumStatus.PendingEmployee, ct);
                count += await _db.PersonnelChangeRequests.AsNoTracking().CountAsync(p => p.EmployeeId == actor.EmployeeId && (p.Status == PersonnelChangeStatus.PendingEmployeeConsent || p.Status == PersonnelChangeStatus.PendingEmployeeExplanation), ct);
            }

            return count;
        }

        private async Task<decimal> CalculateProfileCompletenessAsync(DashboardActor actor, CancellationToken ct)
        {
            var employees = await ScopedEmployees(actor).Take(200).ToListAsync(ct);
            if (employees.Count == 0) return 0m;
            return employees.Average(ProfileCompleteness);
        }

        private IQueryable<Employee> ScopedEmployees(DashboardActor actor)
        {
            var query = _db.Employees.AsNoTracking();

            if (actor.IsCompanyScope) return query;
            if (actor.Role == "Manager" && actor.EmployeeId.HasValue)
            {
                return query.Where(e => e.ManagerId == actor.EmployeeId || (actor.DepartmentId.HasValue && e.DeptId == actor.DepartmentId) || e.Id == actor.EmployeeId);
            }
            if (actor.EmployeeId.HasValue) return query.Where(e => e.Id == actor.EmployeeId);
            return query.Where(e => false);
        }

        private IQueryable<int> ScopedEmployeeIds(DashboardActor actor)
        {
            return ScopedEmployees(actor).Select(e => e.Id);
        }

        private IQueryable<Payroll> ScopedPayrolls(DashboardActor actor)
        {
            var query = _db.Payrolls.AsNoTracking();
            if (actor.IsCompanyScope) return query;
            if (actor.EmployeeId.HasValue && actor.Role == "Manager")
            {
                var ids = ScopedEmployeeIds(actor);
                return query.Where(p => p.EmployeeId.HasValue && ids.Contains(p.EmployeeId.Value));
            }
            if (actor.EmployeeId.HasValue) return query.Where(p => p.EmployeeId == actor.EmployeeId);
            return query.Where(p => false);
        }

        private IQueryable<AttendanceDailySummary> ScopedAttendance(DashboardActor actor)
        {
            var query = _db.AttendanceDailySummaries.AsNoTracking();
            if (actor.IsCompanyScope) return query;
            var ids = ScopedEmployeeIds(actor);
            return query.Where(a => ids.Contains(a.EmployeeId));
        }

        private IQueryable<PerformanceReview> ScopedPerformanceReviews(DashboardActor actor)
        {
            var query = _db.PerformanceReviews.AsNoTracking();
            if (actor.IsCompanyScope) return query;
            var ids = ScopedEmployeeIds(actor);
            return query.Where(p => ids.Contains(p.EmployeeId));
        }

        private IQueryable<Contract> ScopedContracts(DashboardActor actor)
        {
            var query = _db.Contracts.AsNoTracking();
            if (actor.IsCompanyScope) return query;
            var ids = ScopedEmployeeIds(actor);
            return query.Where(c => c.EmployeeId.HasValue && ids.Contains(c.EmployeeId.Value));
        }

        private IQueryable<PersonnelChangeRequest> ScopedPersonnelChanges(DashboardActor actor)
        {
            var query = _db.PersonnelChangeRequests.AsNoTracking();
            if (actor.IsCompanyScope) return query;
            var ids = ScopedEmployeeIds(actor);
            return query.Where(p => p.EmployeeId.HasValue && ids.Contains(p.EmployeeId.Value));
        }

        private async Task<DashboardActor> ResolveActorAsync(int accountId, string role, CancellationToken ct)
        {
            var account = await _db.Accounts.AsNoTracking()
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Id == accountId, ct);

            var employee = await _db.Employees.AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.AccountId == accountId, ct);

            var resolvedRole = NormalizeRole(role);
            if (string.IsNullOrWhiteSpace(resolvedRole))
            {
                resolvedRole = NormalizeRole(account?.Role?.RoleName);
            }

            if (resolvedRole == "Employee" && employee?.Type == EmployeeType.Intern)
            {
                resolvedRole = "Intern";
            }
            else if (resolvedRole == "Employee" && employee?.Type == EmployeeType.Contractual)
            {
                resolvedRole = "Collaborator";
            }

            var scopeLabel = resolvedRole switch
            {
                "Admin" => "Toàn hệ thống",
                "Director" => "Toàn công ty",
                "HR" => "Toàn công ty",
                "Manager" => employee?.Department?.DeptName ?? "Phòng ban quản lý",
                "Candidate" => "Hồ sơ ứng tuyển của tôi",
                _ => "Cá nhân"
            };

            return new DashboardActor(
                accountId,
                resolvedRole,
                account?.Email ?? string.Empty,
                employee?.Id,
                employee?.DeptId,
                account?.FullName ?? employee?.FullName,
                scopeLabel
            );
        }

        private static string NormalizeRole(string? role)
        {
            var value = (role ?? string.Empty).Trim();
            if (value.Equals("Admin", StringComparison.OrdinalIgnoreCase) || value.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (value.Equals("Director", StringComparison.OrdinalIgnoreCase) || value.Equals("Giám đốc", StringComparison.OrdinalIgnoreCase)) return "Director";
            if (value.Equals("HR", StringComparison.OrdinalIgnoreCase) || value.Equals("HumanResources", StringComparison.OrdinalIgnoreCase) || value.Equals("Human Resource", StringComparison.OrdinalIgnoreCase)) return "HR";
            if (value.Equals("Manager", StringComparison.OrdinalIgnoreCase)) return "Manager";
            if (value.Equals("Candidate", StringComparison.OrdinalIgnoreCase)) return "Candidate";
            if (value.Equals("Collaborator", StringComparison.OrdinalIgnoreCase) || value.Equals("CTV", StringComparison.OrdinalIgnoreCase)) return "Collaborator";
            if (value.Equals("Intern", StringComparison.OrdinalIgnoreCase)) return "Intern";
            return string.IsNullOrWhiteSpace(value) ? string.Empty : "Employee";
        }

        private static DashboardPeriod ResolvePeriod(int? month, int? year)
        {
            var now = DateTime.UtcNow;
            var safeMonth = month is >= 1 and <= 12 ? month.Value : now.Month;
            var safeYear = year is >= 2000 and <= 2100 ? year.Value : now.Year;
            return new DashboardPeriod(safeMonth, safeYear);
        }

        private static string[] PeriodKeys(DashboardPeriod period)
        {
            return new[] { $"{period.Year:D4}-{period.Month:D2}", $"{period.Month:D2}/{period.Year:D4}", period.Label };
        }

        private static DashboardWidgetDto Widget(string id, string title, string value, string? subtitle, string severity, string scope, int order, string drilldownType)
        {
            return new DashboardWidgetDto
            {
                Id = id,
                Title = title,
                Value = value,
                Subtitle = subtitle,
                Severity = severity,
                Scope = scope,
                Order = order,
                Drilldown = new DashboardDrilldownRefDto
                {
                    Type = drilldownType,
                    Scope = scope
                }
            };
        }

        private static DashboardDrilldownDto Drilldown(string type, string scope, string title, IEnumerable<string> columns, IEnumerable<Dictionary<string, string?>> rows)
        {
            return new DashboardDrilldownDto
            {
                Type = type,
                Scope = scope,
                Title = title,
                Table = Table(columns, rows)
            };
        }

        private static DashboardDrilldownDto ForbiddenDrilldown(string type, string scope)
        {
            return Drilldown(type, scope, "Không có quyền xem dữ liệu này", new[] { "Nội dung" }, new[]
            {
                Row(("Nội dung", "Tài khoản hiện tại không có quyền xem nhóm dữ liệu này."))
            });
        }

        private static DashboardTableDto Table(IEnumerable<string> columns, IEnumerable<Dictionary<string, string?>> rows)
        {
            return new DashboardTableDto
            {
                Columns = columns.ToList(),
                Rows = rows.ToList()
            };
        }

        private static Dictionary<string, string?> Row(params (string Key, string? Value)[] values)
        {
            return values.ToDictionary(item => item.Key, item => item.Value);
        }

        private static decimal ProfileCompleteness(Employee employee)
        {
            var checks = new[]
            {
                !string.IsNullOrWhiteSpace(employee.FullName),
                !string.IsNullOrWhiteSpace(employee.EmployeeCode),
                !string.IsNullOrWhiteSpace(employee.PhoneNumber),
                !string.IsNullOrWhiteSpace(employee.PersonalEmail),
                !string.IsNullOrWhiteSpace(employee.CurrentAddress),
                !string.IsNullOrWhiteSpace(employee.IdentityNumber),
                !string.IsNullOrWhiteSpace(employee.TaxCode),
                !string.IsNullOrWhiteSpace(employee.SocialInsCode),
                !string.IsNullOrWhiteSpace(employee.BankAccount),
                employee.DeptId.HasValue,
                employee.PositionId.HasValue,
                employee.JoinedDate.HasValue
            };

            return checks.Count(x => x) * 100m / checks.Length;
        }

        private static List<string> MissingProfileFields(Employee employee)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(employee.PhoneNumber)) missing.Add("SĐT");
            if (string.IsNullOrWhiteSpace(employee.PersonalEmail)) missing.Add("Email");
            if (string.IsNullOrWhiteSpace(employee.CurrentAddress)) missing.Add("Địa chỉ");
            if (string.IsNullOrWhiteSpace(employee.IdentityNumber)) missing.Add("CCCD");
            if (string.IsNullOrWhiteSpace(employee.TaxCode)) missing.Add("MST");
            if (string.IsNullOrWhiteSpace(employee.SocialInsCode)) missing.Add("BHXH");
            if (string.IsNullOrWhiteSpace(employee.BankAccount)) missing.Add("Tài khoản ngân hàng");
            if (!employee.DeptId.HasValue) missing.Add("Phòng ban");
            if (!employee.PositionId.HasValue) missing.Add("Chức danh");
            return missing;
        }

        private static string Percent(int value, int total)
        {
            if (total <= 0) return "0%";
            return $"{value * 100m / total:0.#}%";
        }

        private static string Money(decimal value)
        {
            return $"{value:N0} đ";
        }

        private static string Date(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "-";
        }

        private static string DateTimeLabel(DateTime value)
        {
            return value.ToString("dd/MM/yyyy HH:mm");
        }

        private static string PeriodLabel(byte? month, short? year)
        {
            return month.HasValue && year.HasValue ? $"{month.Value:00}/{year.Value}" : "-";
        }

        private static string AttendanceLabel(AttendanceDailyStatus status) => status switch
        {
            AttendanceDailyStatus.Present => "Có mặt",
            AttendanceDailyStatus.HalfDay => "Nửa ngày",
            AttendanceDailyStatus.PaidLeave => "Nghỉ có lương",
            AttendanceDailyStatus.UnpaidLeave => "Nghỉ không lương",
            AttendanceDailyStatus.Absence => "Vắng mặt",
            AttendanceDailyStatus.Holiday => "Ngày lễ",
            AttendanceDailyStatus.Weekend => "Cuối tuần",
            AttendanceDailyStatus.MaternityLeave => "Nghỉ thai sản",
            AttendanceDailyStatus.SickLeave => "Nghỉ ốm",
            AttendanceDailyStatus.ManualAdjusted => "Đã điều chỉnh",
            _ => status.ToString()
        };

        private static string RecruitmentStatusLabel(RecruitmentRequestStatus status) => status switch
        {
            RecruitmentRequestStatus.PendingHR => "Chờ HR xử lý",
            RecruitmentRequestStatus.PendingDirector => "Chờ giám đốc duyệt",
            RecruitmentRequestStatus.Approved => "Đang tuyển",
            RecruitmentRequestStatus.Rejected => "Từ chối",
            RecruitmentRequestStatus.Closed => "Đã đóng",
            _ => status.ToString()
        };

        private static string CandidateStatusLabel(CandidateStatus status) => status switch
        {
            CandidateStatus.New => "Mới nộp",
            CandidateStatus.Interview_Pending => "Chờ phỏng vấn",
            CandidateStatus.Interview_Passed => "Đạt phỏng vấn",
            CandidateStatus.Offer => "Đề nghị nhận việc",
            CandidateStatus.Hired => "Đã tuyển",
            CandidateStatus.Rejected => "Không phù hợp",
            CandidateStatus.SLA_Expired => "Quá hạn xử lý",
            _ => status.ToString()
        };

        private static string ContractStatusLabel(ContractStatus status) => status switch
        {
            ContractStatus.Draft => "Bản nháp",
            ContractStatus.PendingDept => "Chờ trưởng phòng xác nhận",
            ContractStatus.PendingHR => "Chờ HR soạn",
            ContractStatus.Negotiating => "Đang thương lượng",
            ContractStatus.PendingDirector => "Chờ giám đốc duyệt",
            ContractStatus.ApprovedByDirector => "Đã duyệt",
            ContractStatus.Rejected => "Từ chối",
            ContractStatus.Active => "Đang hiệu lực",
            ContractStatus.Liquidating => "Đang thanh lý",
            ContractStatus.Expired => "Hết hiệu lực",
            ContractStatus.Draft_Cancelled => "Đã hủy nháp",
            ContractStatus.PendingManagerContentReview => "Chờ trưởng phòng duyệt nội dung",
            ContractStatus.PendingEmployee => "Chờ nhân viên xác nhận",
            ContractStatus.PendingHRRevision => "Chờ HR chỉnh sửa",
            _ => status.ToString()
        };

        private static string PersonnelChangeTypeLabel(PersonnelChangeType type) => type switch
        {
            PersonnelChangeType.ConvertToOfficial => "Chuyển chính thức",
            PersonnelChangeType.Promotion => "Thăng tiến",
            PersonnelChangeType.SeniorAppointment => "Bổ nhiệm nhân sự cấp cao",
            PersonnelChangeType.VoluntaryTermination => "Nghỉ việc chủ động",
            PersonnelChangeType.Dismissal => "Kỷ luật/sa thải",
            PersonnelChangeType.InternalTransfer => "Thuyên chuyển nội bộ",
            _ => type.ToString()
        };

        private static string PersonnelChangeStatusLabel(PersonnelChangeStatus status) => status switch
        {
            PersonnelChangeStatus.Draft => "Bản nháp",
            PersonnelChangeStatus.PendingHRReview => "Chờ HR xử lý",
            PersonnelChangeStatus.PendingEmployeeConsent => "Chờ nhân viên xác nhận",
            PersonnelChangeStatus.EmployeeDeclined => "Nhân viên từ chối",
            PersonnelChangeStatus.PendingDirectorApproval => "Chờ giám đốc duyệt",
            PersonnelChangeStatus.ApprovedByDirector => "Đã được duyệt",
            PersonnelChangeStatus.PendingContractFlow => "Chờ xử lý hợp đồng",
            PersonnelChangeStatus.ContractNegotiating => "Đang thương lượng hợp đồng",
            PersonnelChangeStatus.ContractAccepted => "Hợp đồng đã chấp thuận",
            PersonnelChangeStatus.ContractRejected => "Hợp đồng bị từ chối",
            PersonnelChangeStatus.PendingDecisionIssuance => "Chờ phát hành quyết định",
            PersonnelChangeStatus.ReadyToExecute => "Sẵn sàng thực thi",
            PersonnelChangeStatus.Completed => "Hoàn tất",
            PersonnelChangeStatus.Rejected => "Từ chối",
            PersonnelChangeStatus.Cancelled => "Đã hủy",
            PersonnelChangeStatus.Escalated => "Đã leo thang",
            PersonnelChangeStatus.PendingCurrentManagerOpinion => "Chờ quản lý hiện tại",
            PersonnelChangeStatus.PendingEmployeeNotification => "Chờ thông báo nhân viên",
            PersonnelChangeStatus.PendingEmployeeExplanation => "Chờ nhân viên giải trình",
            PersonnelChangeStatus.PendingManagerReview => "Chờ quản lý duyệt",
            PersonnelChangeStatus.ContractRevisionClosed => "Đã đóng thương lượng",
            _ => status.ToString()
        };

        private static string TaskStatusLabel(HrmTaskStatus status) => status switch
        {
            HrmTaskStatus.Todo => "Chưa làm",
            HrmTaskStatus.Doing => "Đang làm",
            HrmTaskStatus.Done => "Đã xong",
            HrmTaskStatus.Assigned => "Đã giao",
            HrmTaskStatus.InProgress => "Đang thực hiện",
            HrmTaskStatus.PendingReview => "Chờ đánh giá",
            HrmTaskStatus.ReworkRequired => "Cần làm lại",
            HrmTaskStatus.Completed => "Hoàn tất",
            HrmTaskStatus.AutoApproved => "Tự động duyệt",
            HrmTaskStatus.Overdue => "Quá hạn",
            HrmTaskStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        private static string TrainingStatusLabel(TrainingStatus status) => status switch
        {
            TrainingStatus.InProgress => "Đang học",
            TrainingStatus.Extended => "Gia hạn",
            TrainingStatus.Completed => "Hoàn tất",
            TrainingStatus.PendingEvaluation => "Chờ đánh giá",
            TrainingStatus.Evaluated => "Đã đánh giá",
            TrainingStatus.AutoCompleted => "Tự động hoàn tất",
            TrainingStatus.Failed => "Không đạt",
            TrainingStatus.Overdue => "Quá hạn",
            TrainingStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        private static void AddAdminActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Quản trị truy cập", "/admin/roles-permissions", "open", "shield"),
                Action("Audit log", "/admin/audit-logs", "open", "activity"),
                Action("Cấu hình lương", "/system-config/payroll-policies", "open", "settings")
            });
        }

        private static void AddDirectorActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Phê duyệt", "/approvals", "open", "check"),
                Action("Tổng hợp lương", "/payroll/payroll-aggregation", "open", "wallet"),
                Action("Biến động nhân sự", "/personnel-change/internal-transfer", "open", "users")
            });
        }

        private static void AddHrActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Tạo nhu cầu tuyển dụng", "/recruitment/demands/create", "create", "plus"),
                Action("Soạn hợp đồng", "/employee-contract/hr-contracts", "open", "file"),
                Action("Hồ sơ nhân sự", "/employee-contract/profile-review", "open", "user")
            });
        }

        private static void AddManagerActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Phê duyệt", "/approvals", "open", "check"),
                Action("Chấm KPI", "/tasks/performance-evaluation", "open", "target"),
                Action("Tạo nhu cầu tuyển dụng", "/recruitment/demands/create", "create", "plus")
            });
        }

        private static void AddEmployeeActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Cập nhật hồ sơ", "/employees/my-profile", "open", "user"),
                Action("Tạo đơn nghỉ", "/attendance-leave/leave", "create", "calendar"),
                Action("Đăng ký OT", "/attendance-leave/overtime", "create", "clock")
            });
        }

        private static void AddCollaboratorActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Xem phiếu lương", "/payroll/payslip", "open", "wallet"),
                Action("Công việc của tôi", "/tasks/workspace", "open", "list"),
                Action("Hồ sơ cá nhân", "/employees/my-profile", "open", "user")
            });
        }

        private static void AddCandidateActions(DashboardResponseDto response)
        {
            response.QuickActions.AddRange(new[]
            {
                Action("Vị trí đang tuyển", "/careers", "open", "briefcase"),
                Action("Theo dõi ứng tuyển", "/recruitment/history", "open", "search")
            });
        }

        private static DashboardActionDto Action(string label, string route, string actionType, string icon)
        {
            return new DashboardActionDto
            {
                Label = label,
                Route = route,
                ActionType = actionType,
                Icon = icon
            };
        }

        private sealed record DashboardActor(
            int AccountId,
            string Role,
            string Email,
            int? EmployeeId,
            int? DepartmentId,
            string? DisplayName,
            string ScopeLabel)
        {
            public bool IsCompanyScope => Role is "Admin" or "Director" or "HR";
        }

        private sealed record DashboardPeriod(int Month, int Year)
        {
            public DateTime Start => new(Year, Month, 1);
            public DateTime End => Start.AddMonths(1);
            public string Label => $"{Month:00}/{Year:D4}";
        }
    }
}
