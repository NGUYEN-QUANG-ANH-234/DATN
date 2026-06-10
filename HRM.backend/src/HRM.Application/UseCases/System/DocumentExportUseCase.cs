using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class DocumentExportUseCase : IDocumentExportUseCase
    {
        private const string CacheKey = "DocumentExportTemplateCache_v2";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly HashSet<string> RawHtmlPlaceholders = new(StringComparer.OrdinalIgnoreCase)
        {
            "kpi_detail_rows",
            "payroll_detail_rows",
            "personnel_change_rows"
        };

        private readonly IConfigurationRepository _configRepo;
        private readonly IContractRepository _contractRepo;
        private readonly IContractAddendumRepository _addendumRepo;
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly IOvertimeRequestRepository _overtimeRepo;
        private readonly IRecruitmentRequestRepository _recruitmentRepo;
        private readonly IPerformanceReviewRepository _performanceRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<ProfileUpdateRequest> _profileRequestRepo;
        private readonly IBaseRepository<OnboardingRequest> _onboardingRepo;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        public DocumentExportUseCase(
            IConfigurationRepository configRepo,
            IContractRepository contractRepo,
            IContractAddendumRepository addendumRepo,
            ILeaveRequestRepository leaveRequestRepo,
            IOvertimeRequestRepository overtimeRepo,
            IRecruitmentRequestRepository recruitmentRepo,
            IPerformanceReviewRepository performanceRepo,
            IPayrollRepository payrollRepo,
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<ProfileUpdateRequest> profileRequestRepo,
            IBaseRepository<OnboardingRequest> onboardingRepo,
            IAppCache cache,
            ILockService lockService)
        {
            _configRepo = configRepo;
            _contractRepo = contractRepo;
            _addendumRepo = addendumRepo;
            _leaveRequestRepo = leaveRequestRepo;
            _overtimeRepo = overtimeRepo;
            _recruitmentRepo = recruitmentRepo;
            _performanceRepo = performanceRepo;
            _payrollRepo = payrollRepo;
            _personnelChangeRepo = personnelChangeRepo;
            _employeeRepo = employeeRepo;
            _profileRequestRepo = profileRequestRepo;
            _onboardingRepo = onboardingRepo;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<IEnumerable<DocumentTemplateSummaryDto>> GetAvailableTemplatesAsync(CancellationToken ct = default)
        {
            var templates = await GetTemplateConfigsAsync(ct);
            return templates.Select(t => new DocumentTemplateSummaryDto
            {
                TemplateKey = t.TemplateKey,
                DocumentType = t.DocumentType,
                DisplayName = t.DisplayName,
                ActiveLayoutVersion = t.ActiveLayoutVersion,
                AllowedOutputs = t.AllowedOutputs,
                LayoutVersions = t.LayoutVersions.Select(v => new DocumentLayoutVersionDto
                {
                    Version = v.Version,
                    Name = v.Name,
                    IsActive = v.IsActive
                }).ToList()
            });
        }

        public async Task<DocumentExportResultDto> ExportAsync(string templateKey, int referenceId, string? layoutVersion = null, CancellationToken ct = default)
        {
            var normalizedKey = NormalizeTemplateKey(templateKey);
            var templates = await GetTemplateConfigsAsync(ct);
            var template = templates.FirstOrDefault(t => string.Equals(t.TemplateKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Document template is not configured.");

            var layout = ResolveLayout(template, layoutVersion);
            var data = await BuildDataAsync(normalizedKey, referenceId, ct);

            var content = WrapHtml(
                template,
                layout,
                Render(layout.HeaderHtml, data),
                Render(layout.BodyHtml, data),
                Render(layout.FooterHtml, data));

            return new DocumentExportResultDto
            {
                FileName = $"{normalizedKey}_{referenceId}_{layout.Version}.html",
                ContentType = "text/html; charset=utf-8",
                Content = content
            };
        }

        private async Task<List<DocumentTemplateConfig>> GetTemplateConfigsAsync(CancellationToken ct)
        {
            return await _cache.GetOrSetWithLockAsync(
                CacheKey,
                async innerCt =>
                {
                    var configs = await _configRepo.FetchDocumentTemplatesAsync(innerCt);
                    return configs
                        .Where(config => config.ParamKey.StartsWith("EXPORT_", StringComparison.OrdinalIgnoreCase))
                        .Select(ParseTemplateConfig)
                        .Where(t => t != null)
                        .Select(t => t!)
                        .ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        private static DocumentTemplateConfig? ParseTemplateConfig(Configuration config)
        {
            var parsed = JsonSerializer.Deserialize<DocumentTemplateConfig>(config.ParamValue, JsonOptions);
            if (parsed == null)
                return null;

            if (string.IsNullOrWhiteSpace(parsed.TemplateKey))
                parsed.TemplateKey = config.ParamKey;

            return parsed;
        }

        private async Task<Dictionary<string, string>> BuildDataAsync(string templateKey, int referenceId, CancellationToken ct)
        {
            var data = BaseData();

            switch (templateKey)
            {
                case "EXPORT_CONTRACT":
                    await FillContractDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_CONTRACT_ADDENDUM":
                    await FillAddendumDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_LEAVE_REQUEST":
                    await FillLeaveDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_OVERTIME_REQUEST":
                    await FillOvertimeDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_PROFILE_UPDATE_REQUEST":
                    await FillProfileUpdateDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_ONBOARDING_PROFILE":
                    await FillOnboardingDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_RECRUITMENT_REQUEST":
                    await FillRecruitmentDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_KPI_REVIEW":
                    await FillKpiReviewDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_PAYSLIP":
                    await FillPayslipDataAsync(data, referenceId, ct);
                    break;
                case "EXPORT_PERSONNEL_CHANGE_DECISION":
                    await FillPersonnelChangeDecisionDataAsync(data, referenceId, ct);
                    break;
                default:
                    throw new InvalidOperationException("Document template is not supported.");
            }

            return data;
        }

        private async Task FillContractDataAsync(Dictionary<string, string> data, int contractId, CancellationToken ct)
        {
            var contract = (await _contractRepo.GetContractsWithDetailsAsync(new List<int> { contractId }, ct)).FirstOrDefault()
                ?? throw new InvalidOperationException("Contract not found.");
            FillEmployee(data, contract.Employee);
            data["contract_number"] = Safe(contract.ContractNumber);
            data["contract_type"] = EnumText(contract.ContractType);
            data["basic_salary"] = Money(contract.BasicSalary);
            data["insurance_salary"] = Money(contract.InsuranceSalary);
            data["salary_percentage"] = $"{contract.SalaryPercentage:0.##}%";
            data["start_date"] = Date(contract.StartDate);
            data["end_date"] = contract.EndDate.HasValue ? Date(contract.EndDate.Value) : "Không xác định";
            data["effective_date"] = Date(contract.StartDate);
            data["created_date"] = Date(DateTime.UtcNow);
            data["status"] = EnumText(contract.Status);

            var snapshot = contract.LegalSnapshots
                .OrderByDescending(s => s.Version)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            data["bonus_policy"] = Safe(snapshot?.BonusPolicy);
            data["kpi_bonus_target"] = snapshot?.KpiBonusTargetAmount.HasValue == true
                ? Money(snapshot.KpiBonusTargetAmount.Value)
                : "Theo mức thưởng KPI tối đa được ghi nhận trong hệ thống";
            data["kpi_bonus_policy_code"] = Safe(snapshot?.KpiBonusPolicyCode);
            data["kpi_bonus_policy_version"] = Safe(snapshot?.KpiBonusPolicyVersionCode);
            data["kpi_score_formula"] = Safe(snapshot?.KpiScoreFormula);
            data["kpi_payout_formula"] = Safe(snapshot?.KpiPayoutFormula);
            data["kpi_bonus_eligibility_rule"] = Safe(snapshot?.KpiBonusEligibilityRule);
            data["kpi_bonus_payment_period"] = Safe(snapshot?.KpiBonusPaymentPeriod);
            data["kpi_bonus_approver_role"] = Safe(snapshot?.KpiBonusApproverRole);
        }

        private async Task FillAddendumDataAsync(Dictionary<string, string> data, int addendumId, CancellationToken ct)
        {
            var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct)
                ?? throw new InvalidOperationException("Contract addendum not found.");
            var contract = addendum.Contract;
            var employee = contract?.Employee;
            if (employee == null && contract?.EmployeeId != null)
                employee = await _employeeRepo.GetProfileByIdAsync(contract.EmployeeId.Value, ct);

            FillEmployee(data, employee);
            data["addendum_number"] = Safe(addendum.AddendumNumber);
            data["contract_number"] = Safe(contract?.ContractNumber);
            data["new_basic_salary"] = addendum.NewBasicSalary.HasValue ? Money(addendum.NewBasicSalary.Value) : "Không thay đổi";
            data["new_insurance_salary"] = addendum.NewInsuranceSalary.HasValue ? Money(addendum.NewInsuranceSalary.Value) : "Không thay đổi";
            data["new_end_date"] = addendum.NewEndDate.HasValue ? Date(addendum.NewEndDate.Value) : "Không thay đổi";
            data["other_changes"] = JsonToFlatText(addendum.OtherChangesJson);
            data["effective_date"] = Date(addendum.EffectiveDate);
            data["created_date"] = Date(addendum.CreatedAt);
            data["status"] = EnumText(addendum.Status);
            data["reject_reason"] = Safe(addendum.RejectReason);
        }

        private async Task FillLeaveDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = await _leaveRequestRepo.GetDetailAsync(requestId, ct)
                ?? throw new InvalidOperationException("Leave request not found.");
            FillEmployee(data, request.Employee);
            data["leave_type"] = Safe(request.LeaveType?.TypeName);
            data["start_date"] = Date(request.StartDate);
            data["end_date"] = Date(request.EndDate);
            data["days"] = CalculateDays(request.StartDate, request.EndDate);
            data["reason"] = Safe(request.Reason);
            data["status"] = EnumText(request.Status);
            data["created_date"] = Date(DateTime.UtcNow);
        }

        private async Task FillOvertimeDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = await _overtimeRepo.GetDetailAsync(requestId, ct)
                ?? throw new InvalidOperationException("Overtime request not found.");
            FillEmployee(data, request.Employee);
            data["work_date"] = Date(request.WorkDate);
            data["start_time"] = Time(request.StartTime);
            data["end_time"] = Time(request.EndTime);
            data["approved_minutes"] = request.ApprovedMinutes.ToString(CultureInfo.InvariantCulture);
            data["actual_ot_minutes"] = request.ActualOtMinutes.ToString(CultureInfo.InvariantCulture);
            data["project_code"] = Safe(request.ProjectCode);
            data["reason"] = Safe(request.Reason);
            data["status"] = EnumText(request.Status);
            data["manager_note"] = Safe(request.ManagerNote);
            data["hr_note"] = Safe(request.HrNote);
            data["created_date"] = Date(request.CreatedAt);
        }

        private async Task FillProfileUpdateDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = await _profileRequestRepo.GetByIdAsync(requestId, ct)
                ?? throw new InvalidOperationException("Profile update request not found.");
            var employee = request.Employee ?? await _employeeRepo.GetProfileByIdAsync(request.EmployeeId, ct);
            FillEmployee(data, employee);

            var requestedValues = ParseJsonMap(request.RequestedDataJson);
            data["requested_fields"] = string.Join(", ", requestedValues.Keys);
            data["old_values"] = employee == null
                ? string.Empty
                : string.Join("; ", requestedValues.Keys.Select(k => $"{k}: {GetEmployeeValue(employee, k)}"));
            data["new_values"] = string.Join("; ", requestedValues.Select(kv => $"{kv.Key}: {kv.Value}"));
            data["created_date"] = Date(request.CreatedAt);
            data["status"] = EnumText(request.Status);
            data["reject_reason"] = Safe(request.RejectReason);
            data["reviewed_by"] = string.Empty;
            data["reviewed_date"] = string.Empty;
        }

        private async Task FillOnboardingDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = await _onboardingRepo.GetByIdAsync(requestId, ct)
                ?? throw new InvalidOperationException("Onboarding request not found.");
            var values = ParseJsonMap(request.RequestedDataJson);

            data["candidate_name"] = Safe(request.Candidate?.FullName);
            data["candidate_email"] = Safe(request.Candidate?.Email);
            data["employee_name"] = GetFirst(values, "fullName", "FullName", "employeeName", "EmployeeName");
            data["employee_code"] = GetFirst(values, "employeeCode", "EmployeeCode");
            data["department_name"] = GetFirst(values, "departmentName", "DepartmentName");
            data["position_name"] = GetFirst(values, "positionName", "PositionName");
            data["role_name"] = GetFirst(values, "roleName", "RoleName");
            data["employee_type"] = GetFirst(values, "type", "employeeType", "EmployeeType");
            data["identity_number"] = GetFirst(values, "identityNumber", "IdentityNumber");
            data["phone_number"] = GetFirst(values, "phoneNumber", "PhoneNumber");
            data["personal_email"] = GetFirst(values, "personalEmail", "PersonalEmail");
            data["status"] = EnumText(request.Status);
            data["created_date"] = Date(request.CreatedAt);
            data["reviewed_by"] = string.Empty;
            data["reviewed_date"] = string.Empty;
        }

        private async Task FillRecruitmentDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = (await _recruitmentRepo.GetRequestsWithDetailsAsync(new List<int> { requestId }, ct)).FirstOrDefault()
                ?? throw new InvalidOperationException("Recruitment request not found.");

            data["request_code"] = $"REQ-{request.Id:00000}";
            data["department_name"] = Safe(request.Department?.DeptName);
            data["position_name"] = Safe(request.Position?.Title);
            data["quantity"] = request.Quantity.ToString(CultureInfo.InvariantCulture);
            data["expected_start_date"] = Date(request.Deadline);
            data["reason"] = Safe(request.Description);
            data["description"] = Safe(request.Description);
            data["status"] = EnumText(request.Status);
            data["created_by"] = request.CreatedById.ToString(CultureInfo.InvariantCulture);
            data["created_date"] = Date(request.CreatedAt);
        }

        private async Task FillKpiReviewDataAsync(Dictionary<string, string> data, int reviewId, CancellationToken ct)
        {
            var review = await _performanceRepo.GetDetailAsync(reviewId, ct)
                ?? throw new InvalidOperationException("KPI review not found.");
            FillEmployee(data, review.Employee);
            data["period"] = Safe(review.Period);
            data["total_weight"] = review.TotalWeight.ToString(CultureInfo.InvariantCulture);
            data["total_penalty_points"] = review.Details.Sum(d => d.PenaltyPoint).ToString("0.##", CultureInfo.InvariantCulture);
            data["total_score"] = review.TotalScore.ToString("0.##", CultureInfo.InvariantCulture);
            data["scoring_version"] = Safe(review.ScoringVersion);
            data["final_rating"] = Safe(review.FinalRating);
            data["final_comment"] = Safe(review.FinalComment);
            data["status"] = EnumText(review.Status);
            data["created_date"] = Date(review.CreatedAt);
            data["kpi_detail_rows"] = string.Join("", review.Details.Select(d =>
                $"<tr><td>{Cell(d.KpiCode)}</td><td>{Cell(d.KpiName)}</td><td>{d.WeightPercent}</td><td>{Number(d.TargetValue)}</td><td>{Number(d.ActualValue)}</td><td>{d.AchievedPercent:0.##}</td><td>{d.EmployeeSelfPercent:0.##}</td><td>{d.ManagerScore:0.##}</td><td>{d.PenaltyPoint:0.##}</td><td>{Cell(d.PenaltyReason)}</td><td>{d.FinalPoint:0.##}</td></tr>"));
        }

        private async Task FillPayslipDataAsync(Dictionary<string, string> data, int payrollId, CancellationToken ct)
        {
            var payroll = await _payrollRepo.GetDetailAsync(payrollId, ct)
                ?? throw new InvalidOperationException("Payroll slip not found.");

            FillEmployee(data, payroll.Employee);
            data["period"] = Safe(payroll.Period) != string.Empty
                ? Safe(payroll.Period)
                : $"{payroll.Month:00}/{payroll.Year}";
            data["month"] = payroll.Month?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            data["year"] = payroll.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            data["base_salary"] = Money(payroll.BaseSalary);
            data["gross_income"] = Money(payroll.GrossIncome ?? payroll.GrossSalary);
            data["total_allowance"] = Money(payroll.TotalAllowance);
            data["total_bonus"] = Money(payroll.TotalBonus);
            data["insurance_salary"] = Money(payroll.InsuranceSalary);
            data["employee_insurance_amount"] = Money(payroll.EmployeeInsuranceAmount ?? payroll.InsuranceDeduction);
            data["pit_amount"] = Money(payroll.PitAmount);
            data["other_deductions"] = Money(payroll.OtherDeductions);
            data["net_salary"] = Money(payroll.NetSalary);
            data["actual_workdays"] = Number(payroll.ActualWorkDays);
            data["actual_work_hours"] = Number(payroll.ActualWorkHours);
            data["status"] = EnumText(payroll.Status);
            data["created_date"] = Date(payroll.CreatedAt);
            data["payroll_detail_rows"] = string.Join("", payroll.Details.Select(detail =>
                $"<tr><td>{Cell(detail.ComponentCode)}</td><td>{Cell(detail.ComponentName)}</td><td style=\"text-align:right\">{Money(detail.Amount)}</td><td style=\"text-align:right\">{Money(detail.TaxableAmount)}</td><td style=\"text-align:right\">{Money(detail.InsuranceBaseAmount)}</td><td>{Cell(detail.Note)}</td></tr>"));
        }

        private async Task FillPersonnelChangeDecisionDataAsync(Dictionary<string, string> data, int requestId, CancellationToken ct)
        {
            var request = await _personnelChangeRepo.GetDetailAsync(requestId, ct)
                ?? throw new InvalidOperationException("Personnel change request not found.");

            FillEmployee(data, request.Employee);
            data["request_code"] = $"PC-{request.Id:00000}";
            data["change_type"] = EnumText(request.ChangeType);
            data["promotion_type"] = EnumText(request.PromotionType);
            data["status"] = EnumText(request.Status);
            data["reason"] = Safe(request.Reason);
            data["effective_date"] = Date(request.EffectiveDate);
            data["requested_date"] = Date(request.RequestedAt);
            data["decision_number"] = Safe(request.DecisionNumber);
            data["decision_issued_at"] = Date(request.DecisionIssuedAt);
            data["current_department"] = Safe(request.CurrentDepartment?.DeptName);
            data["new_department"] = Safe(request.NewDepartment?.DeptName);
            data["current_position"] = Safe(request.CurrentPosition?.Title);
            data["new_position"] = Safe(request.NewPosition?.Title);
            data["current_manager"] = Safe(request.CurrentManager?.FullName);
            data["new_manager"] = Safe(request.NewManager?.FullName);
            data["current_job_level"] = Safe(request.CurrentJobLevel?.Name);
            data["new_job_level"] = Safe(request.NewJobLevel?.Name);
            data["current_employee_type"] = EnumText(request.CurrentEmployeeType);
            data["new_employee_type"] = EnumText(request.NewEmployeeType);
            data["director_note"] = Safe(request.DirectorNote);
            data["hr_note"] = Safe(request.HRNote);
            data["manager_note"] = Safe(request.ManagerNote);
            data["employee_note"] = Safe(request.EmployeeConsentNote ?? request.EmployeeExplanation);
            data["personnel_change_rows"] = BuildPersonnelChangeRows(request);
        }

        private static string BuildPersonnelChangeRows(PersonnelChangeRequest request)
        {
            var rows = new List<(string Label, string OldValue, string NewValue)>
            {
                ("Phòng ban", Safe(request.CurrentDepartment?.DeptName), Safe(request.NewDepartment?.DeptName)),
                ("Vị trí", Safe(request.CurrentPosition?.Title), Safe(request.NewPosition?.Title)),
                ("Quản lý trực tiếp", Safe(request.CurrentManager?.FullName), Safe(request.NewManager?.FullName)),
                ("Cấp bậc", Safe(request.CurrentJobLevel?.Name), Safe(request.NewJobLevel?.Name)),
                ("Loại nhân sự", EnumText(request.CurrentEmployeeType), EnumText(request.NewEmployeeType))
            };

            return string.Join("", rows
                .Where(row => !string.IsNullOrWhiteSpace(row.OldValue) || !string.IsNullOrWhiteSpace(row.NewValue))
                .Select(row => $"<tr><td>{Cell(row.Label)}</td><td>{Cell(row.OldValue)}</td><td>{Cell(row.NewValue)}</td></tr>"));
        }

        private static void FillEmployee(Dictionary<string, string> data, Employee? employee)
        {
            data["employee_name"] = Safe(employee?.FullName);
            data["employee_code"] = Safe(employee?.EmployeeCode);
            data["employee_identity_number"] = Safe(employee?.IdentityNumber);
            data["identity_number"] = Safe(employee?.IdentityNumber);
            data["employee_address"] = Safe(employee?.CurrentAddress);
            data["phone_number"] = Safe(employee?.PhoneNumber);
            data["personal_email"] = Safe(employee?.PersonalEmail);
            data["department_name"] = Safe(employee?.Department?.DeptName);
            data["position_name"] = Safe(employee?.Position?.Title);
            data["employee_type"] = EnumText(employee?.Type);
        }

        private static Dictionary<string, string> BaseData()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["company_name"] = "HRM HICAS",
                ["company_address"] = "Dia chi cong ty",
                ["company_tax_code"] = "",
                ["company_logo_url"] = "",
                ["director_name"] = "Giám đốc",
                ["created_by"] = "",
                ["created_date"] = Date(DateTime.UtcNow)
            };
        }

        private static string Render(string? template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            return Regex.Replace(template, "\\{([a-zA-Z0-9_]+)\\}", match =>
            {
                var key = match.Groups[1].Value;
                if (!values.TryGetValue(key, out var value))
                    return string.Empty;

                if (RawHtmlPlaceholders.Contains(key))
                    return value;

                return WebUtility.HtmlEncode(value).Replace("\n", "<br/>");
            });
        }

        private static string WrapHtml(DocumentTemplateConfig template, DocumentLayoutConfig layout, string header, string body, string footer)
        {
            var page = layout.Page ?? new DocumentPageConfig();
            var theme = layout.Theme ?? new DocumentThemeConfig();
            var fontFamily = string.IsNullOrWhiteSpace(theme.FontFamily) ? "Times New Roman" : theme.FontFamily;
            var fontSize = string.IsNullOrWhiteSpace(theme.FontSize) ? "12pt" : theme.FontSize;
            var margin = string.IsNullOrWhiteSpace(page.Margin) ? "20mm" : page.Margin;

            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            builder.AppendLine($"<title>{WebUtility.HtmlEncode(template.DisplayName)}</title>");
            builder.AppendLine("<style>");
            builder.AppendLine($"@page {{ size: {page.Size} {page.Orientation}; margin: {margin}; }}");
            builder.AppendLine($"body {{ font-family: '{fontFamily}', serif; font-size: {fontSize}; color: {theme.PrimaryColor}; line-height: 1.45; }}");
            builder.AppendLine("h1,h2,h3 { margin: 12px 0; } table { border-collapse: collapse; } td, th { padding: 6px; }");
            builder.AppendLine(".document-footer { margin-top: 32px; }");
            builder.AppendLine("</style></head><body>");
            builder.AppendLine($"<header>{header}</header>");
            builder.AppendLine($"<main>{body}</main>");
            builder.AppendLine($"<footer class=\"document-footer\">{footer}</footer>");
            builder.AppendLine("</body></html>");
            return builder.ToString();
        }

        private static DocumentLayoutConfig ResolveLayout(DocumentTemplateConfig template, string? layoutVersion)
        {
            var selectedVersion = string.IsNullOrWhiteSpace(layoutVersion)
                ? template.ActiveLayoutVersion
                : layoutVersion;

            var layout = template.LayoutVersions.FirstOrDefault(v => string.Equals(v.Version, selectedVersion, StringComparison.OrdinalIgnoreCase))
                ?? template.LayoutVersions.FirstOrDefault(v => v.IsActive)
                ?? template.LayoutVersions.FirstOrDefault();

            return layout ?? throw new InvalidOperationException("Document template has no layout version.");
        }

        private static string NormalizeTemplateKey(string templateKey)
        {
            var key = templateKey.Trim().ToUpperInvariant();
            return key.StartsWith("EXPORT_") ? key : $"EXPORT_{key}";
        }

        private static Dictionary<string, string> ParseJsonMap(string? json)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return result;

                foreach (var prop in doc.RootElement.EnumerateObject())
                    result[prop.Name] = JsonElementToString(prop.Value);
            }
            catch (JsonException)
            {
                result["raw"] = json;
            }

            return result;
        }

        private static string JsonToFlatText(string? json)
        {
            var values = ParseJsonMap(json);
            return values.Count == 0
                ? string.Empty
                : string.Join("; ", values.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        private static string JsonElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => element.GetRawText()
            };
        }

        private static string GetFirst(IReadOnlyDictionary<string, string> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private static string GetEmployeeValue(Employee employee, string key)
        {
            return key.ToLowerInvariant() switch
            {
                "fullname" or "full_name" => Safe(employee.FullName),
                "phonenumber" or "phone_number" => Safe(employee.PhoneNumber),
                "personalemail" or "personal_email" => Safe(employee.PersonalEmail),
                "identitynumber" or "identity_number" => Safe(employee.IdentityNumber),
                "currentaddress" or "current_address" => Safe(employee.CurrentAddress),
                "permanentaddress" or "permanent_address" => Safe(employee.PermanentAddress),
                "bankaccount" or "bank_account" => Safe(employee.BankAccount),
                "bankname" or "bank_name" => Safe(employee.BankName),
                "taxcode" or "tax_code" => Safe(employee.TaxCode),
                _ => string.Empty
            };
        }

        private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        private static string Cell(string? value) => WebUtility.HtmlEncode(Safe(value));
        private static string EnumText(object? value) => value?.ToString() ?? string.Empty;
        private static string Money(decimal value) => value.ToString("#,##0", CultureInfo.InvariantCulture);
        private static string Money(decimal? value) => value.HasValue ? Money(value.Value) : string.Empty;
        private static string Number(decimal? value) => value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        private static string Date(DateTime? value) => value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty;
        private static string Time(TimeSpan value) => value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

        private static string CalculateDays(DateTime? start, DateTime? end)
        {
            if (!start.HasValue || !end.HasValue)
                return string.Empty;

            return Math.Max(1, (end.Value.Date - start.Value.Date).Days + 1).ToString(CultureInfo.InvariantCulture);
        }

        private sealed class DocumentTemplateConfig
        {
            public string TemplateKey { get; set; } = string.Empty;
            public string DocumentType { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string DefaultOutput { get; set; } = "PDF";
            public string ActiveLayoutVersion { get; set; } = string.Empty;
            public List<string> AllowedOutputs { get; set; } = new();
            public List<DocumentLayoutConfig> LayoutVersions { get; set; } = new();
        }

        private sealed class DocumentLayoutConfig
        {
            public string Version { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DocumentPageConfig? Page { get; set; }
            public DocumentThemeConfig? Theme { get; set; }
            public string HeaderHtml { get; set; } = string.Empty;
            public string BodyHtml { get; set; } = string.Empty;
            public string FooterHtml { get; set; } = string.Empty;
        }

        private sealed class DocumentPageConfig
        {
            public string Size { get; set; } = "A4";
            public string Orientation { get; set; } = "portrait";
            public string Margin { get; set; } = "20mm";
        }

        private sealed class DocumentThemeConfig
        {
            public string FontFamily { get; set; } = "Times New Roman";
            public string FontSize { get; set; } = "12pt";
            public string PrimaryColor { get; set; } = "#111827";
            public string AccentColor { get; set; } = "#1d4ed8";
            public string LogoUrl { get; set; } = string.Empty;
        }
    }
}
