using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class DocumentTemplateManagementUseCase : IDocumentTemplateManagementUseCase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static readonly Regex PlaceholderRegex = new(@"\{([a-z][a-z0-9_]*)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string CompanyLegalName = "CÔNG TY TNHH PHẦN MỀM HICAS";
        private const string CompanyEnglishName = "HICAS SOFTWARE COMPANY LIMITED";
        private const string CompanyTaxCode = "0106695753";
        private const string CompanyPhone = "(+84) 966 939 050";
        private const string CompanyEmail = "vn@hicas.co";
        private const string CompanyEstablishedDate = "21/11/2014";
        private const string CompanyRepresentativeName = "Vũ Thành Nam";
        private const string CompanyRepresentativeTitle = "Tổng giám đốc";
        private const string CompanyBusinessLine = "Xuất bản phần mềm, Engineering Software, CAD/BIM, ERP, AI/Data và chuyển đổi số doanh nghiệp";
        private const string CompanyHeadOfficeAddress = "Tầng 6, Tòa B1 - Roman Plaza, P. Tố Hữu, Nam Từ Liêm, Hà Nội";
        private const string CompanyRegisteredAddress = "HPPB1-0604 - 0602C, Tầng 6, Tòa B1, Tổ hợp thương mại, dịch vụ và căn hộ cao cấp Hải Phát Plaza, Phường Xuân Phương, Hà Nội";
        private const string CompanyHcmOfficeAddress = "Tầng 3, 86/59 Đường Phổ Quang, TP. Hồ Chí Minh";

        private static readonly HashSet<string> BuiltInPlaceholders = new(StringComparer.OrdinalIgnoreCase)
        {
            "company_name",
            "company_legal_name",
            "company_english_name",
            "company_address",
            "company_registered_address",
            "company_hcm_office",
            "company_tax_code",
            "company_phone",
            "company_email",
            "company_established_date",
            "company_representative_name",
            "company_representative_title",
            "company_business_line",
            "company_logo_url",
            "document_title",
            "document_number",
            "issued_date",
            "issued_date_text",
            "issued_place",
            "signer_name"
        };

        private readonly IConfigurationRepository _configRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IUnitOfWork _unitOfWork;

        public DocumentTemplateManagementUseCase(
            IConfigurationRepository configRepo,
            IEmployeeRepository employeeRepo,
            IUnitOfWork unitOfWork)
        {
            _configRepo = configRepo;
            _employeeRepo = employeeRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyCollection<DocumentTemplateConfigDto>> GetTemplatesAsync(bool includeInactive = true, CancellationToken ct = default)
        {
            await EnsureDefaultTemplatesAsync(ct);
            var configs = await _configRepo.FetchDocumentTemplatesAsync(ct);

            return configs
                .Where(config => !IsLegacyExportTemplate(config.ParamKey))
                .Select(ParseTemplate)
                .Where(template => template != null)
                .Select(template => NormalizeTemplate(template!))
                .Where(template => includeInactive || IsActive(template))
                .OrderBy(template => template.Category)
                .ThenBy(template => template.DisplayName)
                .ToList();
        }

        public async Task<DocumentTemplateConfigDto> GetTemplateAsync(string templateKey, CancellationToken ct = default)
        {
            var normalizedKey = NormalizeTemplateKey(templateKey);
            var template = (await GetTemplatesAsync(true, ct))
                .FirstOrDefault(item => string.Equals(item.TemplateKey, normalizedKey, StringComparison.OrdinalIgnoreCase));

            return template ?? throw new InvalidOperationException("Không tìm thấy cấu hình biểu mẫu.");
        }

        public Task<IReadOnlyCollection<DocumentFieldCatalogDto>> GetFieldCatalogsAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyCollection<DocumentFieldCatalogDto>>(DefaultFieldCatalogs());
        }

        public async Task<DocumentTemplateConfigDto> SaveTemplateAsync(DocumentTemplateConfigDto dto, int actorId, CancellationToken ct = default)
        {
            var template = NormalizeTemplate(dto);
            if (IsLegacyExportTemplate(template.TemplateKey))
                throw new ArgumentException("Mẫu EXPORT_* thuộc luồng xuất hồ sơ nghiệp vụ cũ, không cấu hình tại trang biểu mẫu tự phục vụ.");

            var validation = await ValidateTemplateAsync(template, ct);
            if (!validation.IsValid)
                throw new ArgumentException($"Biểu mẫu còn placeholder không hợp lệ: {string.Join(", ", validation.InvalidPlaceholders.Concat(validation.MissingFields))}");

            var json = JsonSerializer.Serialize(template, JsonOptions);
            await _configRepo.SaveDocumentTemplateAsync(
                template.TemplateKey,
                json,
                $"Cấu hình biểu mẫu {template.DisplayName}",
                IsActive(template),
                ct);
            await _unitOfWork.CommitAsync(ct);

            return template;
        }

        public Task<DocumentTemplateValidationResultDto> ValidateTemplateAsync(DocumentTemplateConfigDto dto, CancellationToken ct = default)
        {
            var template = NormalizeTemplate(dto);
            var result = new DocumentTemplateValidationResultDto();
            var placeholders = ExtractPlaceholders(template).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fieldCodes = template.Fields
                .Where(field => field.IsActive)
                .Select(field => field.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var catalogPaths = DefaultFieldCatalogs()
                .ToDictionary(item => item.SourcePath, item => item, StringComparer.OrdinalIgnoreCase);

            foreach (var field in template.Fields.Where(field => field.IsActive))
            {
                if (field.BindingType.Equals("System", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(field.SourcePath) || !catalogPaths.ContainsKey(field.SourcePath))
                        result.MissingFields.Add($"{{{field.Code}}}: sourcePath không thuộc danh mục field hệ thống.");
                }

                if (!placeholders.Contains(field.Code))
                    result.UnusedFields.Add($"{{{field.Code}}}");
            }

            var allowed = fieldCodes
                .Concat(BuiltInPlaceholders)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var placeholder in placeholders)
            {
                if (!allowed.Contains(placeholder))
                    result.InvalidPlaceholders.Add($"{{{placeholder}}}");
            }

            if (string.IsNullOrWhiteSpace(template.BodyHtml))
                result.Warnings.Add("Nội dung biểu mẫu đang trống.");

            if (result.UnusedFields.Count > 0)
                result.Warnings.Add("Có field đã khai báo nhưng chưa dùng trong HTML.");

            return Task.FromResult(result);
        }

        public async Task<DocumentTemplatePreviewResultDto> PreviewTemplateAsync(DocumentTemplatePreviewRequestDto request, DocumentActorContextDto actor, CancellationToken ct = default)
        {
            var template = NormalizeTemplate(request.TemplateConfig);
            var validation = await ValidateTemplateAsync(template, ct);
            var values = await ResolveValuesAsync(
                template,
                request.ManualValues,
                request.EmployeeId,
                actor,
                request.PreviewMode,
                strictManual: false,
                ct);

            return new DocumentTemplatePreviewResultDto
            {
                Html = RenderDocument(template, values.Values),
                ResolvedValues = values.Values,
                MissingFields = values.MissingFields.Concat(validation.MissingFields).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                InvalidPlaceholders = validation.InvalidPlaceholders,
                Warnings = values.Warnings.Concat(validation.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        public async Task<IReadOnlyCollection<DocumentFormTemplateSummaryDto>> GetAvailableFormsAsync(DocumentActorContextDto actor, CancellationToken ct = default)
        {
            var templates = await GetTemplatesAsync(false, ct);
            return templates
                .Where(template => CanUseTemplate(template, actor))
                .Select(template => new DocumentFormTemplateSummaryDto
                {
                    TemplateKey = template.TemplateKey,
                    DisplayName = template.DisplayName,
                    Category = template.Category,
                    DocumentTitle = template.DocumentTitle,
                    NumberPrefix = template.NumberPrefix,
                    DataScope = template.DataScope
                })
                .ToList();
        }

        public async Task<DocumentFormPrepareResultDto> PrepareFormAsync(string templateKey, int? employeeId, DocumentActorContextDto actor, CancellationToken ct = default)
        {
            var template = await GetTemplateAsync(templateKey, ct);
            EnsureTemplateAccess(template, actor);

            var values = await ResolveValuesAsync(template, new Dictionary<string, string>(), employeeId, actor, "Real", strictManual: false, ct);

            return new DocumentFormPrepareResultDto
            {
                TemplateKey = template.TemplateKey,
                DisplayName = template.DisplayName,
                Category = template.Category,
                DocumentTitle = template.DocumentTitle,
                NumberPrefix = template.NumberPrefix,
                Fields = template.Fields
                    .Where(field => field.IsActive)
                    .OrderBy(field => field.SortOrder)
                    .ThenBy(field => field.Label)
                    .Select(field => new DocumentFormFieldPrepareDto
                    {
                        Code = field.Code,
                        Label = field.Label,
                        BindingType = field.BindingType,
                        DataType = field.DataType,
                        Required = field.Required,
                        Value = values.Values.TryGetValue(field.Code, out var value) ? value : string.Empty,
                        ReadOnly = !field.BindingType.Equals("Manual", StringComparison.OrdinalIgnoreCase),
                        Options = field.Options,
                        SortOrder = field.SortOrder
                    })
                    .ToList(),
                PreviewHtml = RenderDocument(template, values.Values),
                ResolvedValues = values.Values,
                Warnings = values.Warnings
            };
        }

        public async Task<DocumentFormGenerateResultDto> GenerateFormAsync(string templateKey, DocumentFormGenerateRequestDto request, DocumentActorContextDto actor, CancellationToken ct = default)
        {
            var template = await GetTemplateAsync(templateKey, ct);
            EnsureTemplateAccess(template, actor);
            var validation = await ValidateTemplateAsync(template, ct);
            if (!validation.IsValid)
                throw new ArgumentException("Biểu mẫu chưa hợp lệ, vui lòng kiểm tra cấu hình trước khi xuất.");

            var values = await ResolveValuesAsync(template, request.ManualValues, request.EmployeeId, actor, "Real", strictManual: true, ct);
            if (values.MissingFields.Count > 0)
                throw new ArgumentException($"Vui lòng nhập đủ thông tin: {string.Join(", ", values.MissingFields)}");

            var html = RenderDocument(template, values.Values);
            return new DocumentFormGenerateResultDto
            {
                FileName = $"{template.TemplateKey}_{DateTime.UtcNow:yyyyMMddHHmmss}.html",
                ContentType = "text/html; charset=utf-8",
                Content = html,
                ResolvedValues = values.Values,
                Warnings = values.Warnings
            };
        }

        private async Task EnsureDefaultTemplatesAsync(CancellationToken ct)
        {
            var defaults = HicasDefaultTemplates()
                .Select(template => (
                    template.TemplateKey,
                    JsonSerializer.Serialize(NormalizeTemplate(template), JsonOptions),
                    $"Biểu mẫu mặc định {template.DisplayName}",
                    true))
                .ToList();

            await _configRepo.EnsureDocumentTemplatesAsync(defaults, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        private static DocumentTemplateConfigDto? ParseTemplate(Configuration config)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<DocumentTemplateConfigDto>(config.ParamValue, JsonOptions);
                if (parsed == null)
                    return null;

                if (string.IsNullOrWhiteSpace(parsed.TemplateKey))
                    parsed.TemplateKey = config.ParamKey;

                if (!config.IsActive)
                    parsed.Status = "Inactive";

                return parsed;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<ResolvedDocumentValues> ResolveValuesAsync(
            DocumentTemplateConfigDto template,
            IReadOnlyDictionary<string, string> manualValues,
            int? requestedEmployeeId,
            DocumentActorContextDto actor,
            string previewMode,
            bool strictManual,
            CancellationToken ct)
        {
            var result = new ResolvedDocumentValues();
            var now = DateTime.UtcNow;
            var employee = previewMode.Equals("Sample", StringComparison.OrdinalIgnoreCase)
                ? null
                : await ResolveEmployeeAsync(template, requestedEmployeeId, actor, ct);

            foreach (var item in BaseValues(template, now))
                result.Values[item.Key] = item.Value;

            foreach (var field in template.Fields.Where(field => field.IsActive).OrderBy(field => field.SortOrder))
            {
                var bindingType = NormalizeBindingType(field.BindingType);
                if (bindingType == "Manual")
                {
                    manualValues.TryGetValue(field.Code, out var manualValue);
                    var value = manualValue ?? field.DefaultValue ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(value) && !strictManual)
                        value = previewMode.Equals("Sample", StringComparison.OrdinalIgnoreCase)
                            ? $"[{field.Label}]"
                            : string.Empty;

                    if (field.Required && strictManual && string.IsNullOrWhiteSpace(value))
                        result.MissingFields.Add(field.Label);

                    result.Values[field.Code] = value;
                    continue;
                }

                if (bindingType == "Computed")
                {
                    result.Values[field.Code] = ComputeValue(field, template, manualValues, result.Values, now);
                    continue;
                }

                result.Values[field.Code] = previewMode.Equals("Sample", StringComparison.OrdinalIgnoreCase)
                    ? SampleValue(field)
                    : ResolveSystemValue(field.SourcePath, employee);
            }

            if (!previewMode.Equals("Sample", StringComparison.OrdinalIgnoreCase) && employee == null)
                result.Warnings.Add("Không tìm thấy hồ sơ nhân viên liên kết với tài khoản hiện tại, một số field hệ thống có thể để trống.");

            return result;
        }

        private async Task<Employee?> ResolveEmployeeAsync(DocumentTemplateConfigDto template, int? requestedEmployeeId, DocumentActorContextDto actor, CancellationToken ct)
        {
            var currentEmployee = await _employeeRepo.GetDocumentProfileByAccountIdAsync(actor.AccountId, ct);
            if (!requestedEmployeeId.HasValue)
                return currentEmployee;

            var canUseOtherEmployee = HasAnyRole(actor, "Admin", "HR");
            if (!canUseOtherEmployee && currentEmployee?.Id != requestedEmployeeId.Value)
                throw new UnauthorizedAccessException("Bạn chỉ được xuất biểu mẫu cho chính mình.");

            if (template.DataScope.Equals("SELF", StringComparison.OrdinalIgnoreCase) &&
                !canUseOtherEmployee &&
                currentEmployee?.Id != requestedEmployeeId.Value)
                throw new UnauthorizedAccessException("Biểu mẫu này chỉ cho phép dùng dữ liệu cá nhân.");

            return await _employeeRepo.GetDocumentProfileByIdAsync(requestedEmployeeId.Value, ct);
        }

        private static Dictionary<string, string> BaseValues(DocumentTemplateConfigDto template, DateTime now)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["company_name"] = CompanyLegalName,
                ["company_legal_name"] = CompanyLegalName,
                ["company_english_name"] = CompanyEnglishName,
                ["company_address"] = CompanyHeadOfficeAddress,
                ["company_registered_address"] = CompanyRegisteredAddress,
                ["company_hcm_office"] = CompanyHcmOfficeAddress,
                ["company_tax_code"] = CompanyTaxCode,
                ["company_phone"] = CompanyPhone,
                ["company_email"] = CompanyEmail,
                ["company_established_date"] = CompanyEstablishedDate,
                ["company_representative_name"] = CompanyRepresentativeName,
                ["company_representative_title"] = CompanyRepresentativeTitle,
                ["company_business_line"] = CompanyBusinessLine,
                ["company_logo_url"] = string.Empty,
                ["document_title"] = template.DocumentTitle,
                ["document_number"] = BuildDocumentNumber(template, now),
                ["issued_date"] = now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                ["issued_date_text"] = $"ngày {now:dd} tháng {now:MM} năm {now:yyyy}",
                ["issued_place"] = "Hà Nội",
                ["signer_name"] = CompanyRepresentativeName
            };
        }

        private static string ResolveSystemValue(string? sourcePath, Employee? employee)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return string.Empty;

            return sourcePath.Trim() switch
            {
                "Employee.FullName" => Safe(employee?.FullName),
                "Employee.EmployeeCode" => Safe(employee?.EmployeeCode),
                "Employee.IdentityNumber" => Safe(employee?.IdentityNumber),
                "Employee.Nationality" => Safe(employee?.Nationality),
                "Employee.Ethnicity" => Safe(employee?.Ethnicity),
                "Employee.PhoneNumber" => Safe(employee?.PhoneNumber),
                "Employee.PersonalEmail" => Safe(employee?.PersonalEmail),
                "Employee.CurrentAddress" => Safe(employee?.CurrentAddress),
                "Employee.PermanentAddress" => Safe(employee?.PermanentAddress),
                "Employee.Department.DeptName" => Safe(employee?.Department?.DeptName),
                "Employee.Position.Title" => Safe(employee?.Position?.Title),
                "Employee.JobLevel.Name" => Safe(employee?.JobLevel?.Name),
                "Employee.Manager.FullName" => Safe(employee?.Manager?.FullName),
                "Employee.Type" => employee?.Type.ToString() ?? string.Empty,
                "Employee.Status" => employee?.Status.ToString() ?? string.Empty,
                "Employee.JoinedDate" => Date(employee?.JoinedDate),
                "Employee.BirthDate" => Date(employee?.BirthDate),
                "Contract.Current.ContractNumber" => Safe(CurrentContract(employee)?.ContractNumber),
                "Contract.Current.BasicSalary" => Money(CurrentContract(employee)?.BasicSalary),
                "Contract.Current.InsuranceSalary" => Money(CurrentContract(employee)?.InsuranceSalary),
                "Contract.Current.StartDate" => Date(CurrentContract(employee)?.StartDate),
                "Contract.Current.EndDate" => Date(CurrentContract(employee)?.EndDate),
                _ => string.Empty
            };
        }

        private static string ComputeValue(
            DocumentTemplateFieldDto field,
            DocumentTemplateConfigDto template,
            IReadOnlyDictionary<string, string> manualValues,
            IReadOnlyDictionary<string, string> resolvedValues,
            DateTime now)
        {
            var key = field.ResolverKey ?? string.Empty;
            if (key.Equals("System.Today", StringComparison.OrdinalIgnoreCase))
                return now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            if (key.Equals("Document.Number", StringComparison.OrdinalIgnoreCase))
                return BuildDocumentNumber(template, now);

            if (key.Equals("Leave.TotalDays", StringComparison.OrdinalIgnoreCase))
            {
                var from = GetFirst(manualValues, resolvedValues, "leave_from", "start_date");
                var to = GetFirst(manualValues, resolvedValues, "leave_to", "end_date");
                if (DateTime.TryParse(from, out var start) && DateTime.TryParse(to, out var end))
                    return Math.Max(1, (end.Date - start.Date).Days + 1).ToString(CultureInfo.InvariantCulture);
            }

            return field.DefaultValue ?? "[Backend tính]";
        }

        private static string RenderDocument(DocumentTemplateConfigDto template, IReadOnlyDictionary<string, string> values)
        {
            var header = RenderTemplate(template.HeaderHtml, values);
            var body = RenderTemplate(template.BodyHtml, values);
            var footer = RenderTemplate(template.FooterHtml, values);
            var layout = template.Layout ?? new DocumentTemplateLayoutDto();
            var fontFamily = string.IsNullOrWhiteSpace(layout.FontFamily) ? "Times New Roman" : layout.FontFamily;
            var fontSize = string.IsNullOrWhiteSpace(layout.FontSize) ? "12pt" : layout.FontSize;
            var margin = string.IsNullOrWhiteSpace(layout.Margin) ? "20mm" : layout.Margin;
            var orientation = string.IsNullOrWhiteSpace(layout.Orientation) ? "portrait" : layout.Orientation;
            var pageSize = string.IsNullOrWhiteSpace(layout.PageSize) ? "A4" : layout.PageSize;

            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            builder.AppendLine($"<title>{WebUtility.HtmlEncode(template.DisplayName)}</title>");
            builder.AppendLine("<style>");
            builder.AppendLine($"@page {{ size: {pageSize} {orientation}; margin: {margin}; }}");
            builder.AppendLine("body { margin: 0; background: #f1f5f9; color: #111827; }");
            builder.AppendLine($".doc-page {{ width: 794px; min-height: 1123px; margin: 0 auto; padding: 46px 58px; box-sizing: border-box; background: #fff; font-family: '{fontFamily}', serif; font-size: {fontSize}; line-height: 1.55; }}");
            builder.AppendLine(".doc-title { margin: 28px 0 18px; text-align: center; font-size: 20px; font-weight: 700; text-transform: uppercase; }");
            builder.AppendLine(".doc-header { display: table; width: 100%; table-layout: fixed; } .doc-header-left,.doc-header-right { display: table-cell; width: 50%; vertical-align: top; } .doc-header-right { text-align: center; font-weight: 700; }");
            builder.AppendLine(".doc-company-row { display: table; width: 100%; table-layout: fixed; } .doc-logo-mark { display: table-cell; width: 58px; height: 42px; border: 1px solid #111827; text-align: center; vertical-align: middle; font-size: 15px; font-weight: 800; letter-spacing: 0; } .doc-company-info { display: table-cell; padding-left: 10px; vertical-align: top; }");
            builder.AppendLine(".doc-small { font-size: 12px; line-height: 1.35; } .doc-body p { margin: 0 0 10px; text-align: justify; } .doc-footer { margin-top: 42px; }");
            builder.AppendLine(".doc-meta { margin-top: 8px; font-size: 12px; line-height: 1.35; } .doc-basis { margin: 0 0 10px; font-style: italic; }");
            builder.AppendLine(".doc-signatures { display: table; width: 100%; table-layout: fixed; margin-top: 42px; } .doc-signature { display: table-cell; width: 50%; text-align: center; vertical-align: top; }");
            builder.AppendLine("</style></head><body>");
            builder.AppendLine("<article class=\"doc-page\">");
            builder.AppendLine(header);
            builder.AppendLine($"<h1 class=\"doc-title\">{WebUtility.HtmlEncode(template.DocumentTitle)}</h1>");
            builder.AppendLine($"<section class=\"doc-body\">{body}</section>");
            builder.AppendLine($"<footer class=\"doc-footer\">{footer}</footer>");
            builder.AppendLine("</article></body></html>");
            return builder.ToString();
        }

        private static string RenderTemplate(string? template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                if (!values.TryGetValue(key, out var value))
                    return string.Empty;

                return WebUtility.HtmlEncode(value).Replace("\n", "<br/>");
            });
        }

        private static DocumentTemplateValidationResultDto ValidateAccessShape(DocumentTemplateConfigDto template)
        {
            var result = new DocumentTemplateValidationResultDto();
            if (string.IsNullOrWhiteSpace(template.TemplateKey))
                result.MissingFields.Add("TemplateKey");
            if (string.IsNullOrWhiteSpace(template.DisplayName))
                result.MissingFields.Add("DisplayName");
            return result;
        }

        private static IReadOnlyCollection<string> ExtractPlaceholders(DocumentTemplateConfigDto template)
        {
            return PlaceholderRegex
                .Matches($"{template.HeaderHtml}\n{template.BodyHtml}\n{template.FooterHtml}")
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DocumentTemplateConfigDto NormalizeTemplate(DocumentTemplateConfigDto template)
        {
            template.TemplateKey = NormalizeTemplateKey(template.TemplateKey);
            template.DisplayName = string.IsNullOrWhiteSpace(template.DisplayName) ? template.TemplateKey : template.DisplayName.Trim();
            template.Category = string.IsNullOrWhiteSpace(template.Category) ? "Biểu mẫu" : template.Category.Trim();
            template.DocumentTitle = string.IsNullOrWhiteSpace(template.DocumentTitle) ? template.DisplayName.ToUpperInvariant() : template.DocumentTitle.Trim();
            template.Status = NormalizeStatus(template.Status);
            template.NumberPrefix = string.IsNullOrWhiteSpace(template.NumberPrefix) ? "DOC" : template.NumberPrefix.Trim().ToUpperInvariant();
            template.SignerTitle = string.IsNullOrWhiteSpace(template.SignerTitle) ? "Người lập" : template.SignerTitle.Trim();
            template.DataScope = NormalizeDataScope(template.DataScope);
            template.AllowedRoles = template.AllowedRoles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (template.AllowedRoles.Count == 0)
                template.AllowedRoles = new List<string> { "Employee", "HR", "Admin" };
            template.AllowedOutputs = template.AllowedOutputs.Count == 0
                ? new List<string> { "HTML", "DOC" }
                : template.AllowedOutputs.Select(item => item.Trim().ToUpperInvariant()).Distinct().ToList();
            template.Layout ??= new DocumentTemplateLayoutDto();
            template.Fields = template.Fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Code))
                .Select((field, index) => NormalizeField(field, index))
                .GroupBy(field => field.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.Label)
                .ToList();
            return template;
        }

        private static DocumentTemplateFieldDto NormalizeField(DocumentTemplateFieldDto field, int index)
        {
            field.Code = field.Code.Trim().ToLowerInvariant();
            field.Label = string.IsNullOrWhiteSpace(field.Label) ? field.Code : field.Label.Trim();
            field.BindingType = NormalizeBindingType(field.BindingType);
            field.DataType = NormalizeDataType(field.DataType);
            field.SourcePath = string.IsNullOrWhiteSpace(field.SourcePath) ? null : field.SourcePath.Trim();
            field.ResolverKey = string.IsNullOrWhiteSpace(field.ResolverKey) ? null : field.ResolverKey.Trim();
            field.DefaultValue = string.IsNullOrWhiteSpace(field.DefaultValue) ? null : field.DefaultValue.Trim();
            field.SortOrder = field.SortOrder == 0 ? index + 1 : field.SortOrder;
            return field;
        }

        private static string NormalizeTemplateKey(string key)
        {
            return key.Trim().ToUpperInvariant();
        }

        private static string NormalizeStatus(string status)
        {
            return status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";
        }

        private static string NormalizeDataScope(string dataScope)
        {
            var scope = dataScope.Trim().ToUpperInvariant();
            return scope is "SELF" or "TEAM" or "ALL" or "RECORD" ? scope : "SELF";
        }

        private static string NormalizeBindingType(string bindingType)
        {
            return bindingType.Trim().Equals("System", StringComparison.OrdinalIgnoreCase)
                ? "System"
                : bindingType.Trim().Equals("Computed", StringComparison.OrdinalIgnoreCase)
                    ? "Computed"
                    : "Manual";
        }

        private static string NormalizeDataType(string dataType)
        {
            var value = dataType.Trim();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Text", "Textarea", "Number", "Money", "Date", "DateTime", "Boolean", "Select", "Time"
            };
            return allowed.Contains(value) ? value : "Text";
        }

        private static bool IsActive(DocumentTemplateConfigDto template)
        {
            return template.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLegacyExportTemplate(string key)
        {
            return key.Trim().StartsWith("EXPORT_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanUseTemplate(DocumentTemplateConfigDto template, DocumentActorContextDto actor)
        {
            if (!IsActive(template))
                return false;

            return template.AllowedRoles.Count == 0 ||
                   template.AllowedRoles.Any(role => actor.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)) ||
                   HasAnyRole(actor, "Admin", "HR");
        }

        private static void EnsureTemplateAccess(DocumentTemplateConfigDto template, DocumentActorContextDto actor)
        {
            if (!CanUseTemplate(template, actor))
                throw new UnauthorizedAccessException("Bạn không có quyền sử dụng biểu mẫu này.");
        }

        private static bool HasAnyRole(DocumentActorContextDto actor, params string[] roles)
        {
            return roles.Any(role => actor.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        private static string SampleValue(DocumentTemplateFieldDto field)
        {
            return field.SourcePath switch
            {
                "Employee.FullName" => "Nguyễn Văn A",
                "Employee.EmployeeCode" => "NV0001",
                "Employee.IdentityNumber" => "001201000001",
                "Employee.PhoneNumber" => "0900000000",
                "Employee.PersonalEmail" => "nguyenvana@example.com",
                "Employee.Department.DeptName" => "Phòng Kỹ thuật",
                "Employee.Position.Title" => "Backend Developer",
                "Employee.JobLevel.Name" => "Senior",
                "Employee.Manager.FullName" => "Trần Thị B",
                "Employee.CurrentAddress" => "Hà Nội",
                "Contract.Current.BasicSalary" => "20,000,000",
                _ => $"[{field.Label}]"
            };
        }

        private static Contract? CurrentContract(Employee? employee)
        {
            return employee?.Contracts
                .OrderByDescending(contract => contract.StartDate)
                .FirstOrDefault();
        }

        private static string BuildDocumentNumber(DocumentTemplateConfigDto template, DateTime now)
        {
            var prefix = string.IsNullOrWhiteSpace(template.NumberPrefix) ? "DOC" : template.NumberPrefix.Trim().ToUpperInvariant();
            return $"{prefix}/{now:yyyy}/{now:MMddHHmmssfff}-HICAS";
        }

        private static string GetFirst(
            IReadOnlyDictionary<string, string> first,
            IReadOnlyDictionary<string, string> second,
            params string[] keys)
        {
            foreach (var key in keys)
            {
                if (first.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value;
                if (second.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        private static string Date(DateTime? value) => value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty;
        private static string Money(decimal? value) => value.HasValue ? value.Value.ToString("#,##0", CultureInfo.InvariantCulture) : string.Empty;

        private static IReadOnlyCollection<DocumentFieldCatalogDto> DefaultFieldCatalogs()
        {
            return new List<DocumentFieldCatalogDto>
            {
                Catalog("employee_name", "Họ tên nhân viên", "Employee.FullName", "Hồ sơ nhân sự"),
                Catalog("employee_code", "Mã nhân viên", "Employee.EmployeeCode", "Hồ sơ nhân sự"),
                Catalog("identity_number", "CCCD/CMND", "Employee.IdentityNumber", "Hồ sơ nhân sự"),
                Catalog("nationality", "Quốc tịch", "Employee.Nationality", "Hồ sơ nhân sự"),
                Catalog("ethnicity", "Dân tộc", "Employee.Ethnicity", "Hồ sơ nhân sự"),
                Catalog("phone_number", "Số điện thoại", "Employee.PhoneNumber", "Hồ sơ nhân sự"),
                Catalog("personal_email", "Email cá nhân", "Employee.PersonalEmail", "Hồ sơ nhân sự"),
                Catalog("current_address", "Địa chỉ hiện tại", "Employee.CurrentAddress", "Hồ sơ nhân sự"),
                Catalog("current_department", "Phòng ban hiện tại", "Employee.Department.DeptName", "Tổ chức"),
                Catalog("current_position", "Chức danh hiện tại", "Employee.Position.Title", "Tổ chức"),
                Catalog("current_job_level", "Cấp bậc hiện tại", "Employee.JobLevel.Name", "Tổ chức"),
                Catalog("direct_manager", "Quản lý trực tiếp", "Employee.Manager.FullName", "Tổ chức"),
                Catalog("employee_type", "Loại nhân sự", "Employee.Type", "Hồ sơ nhân sự"),
                Catalog("joined_date", "Ngày vào làm", "Employee.JoinedDate", "Hồ sơ nhân sự", "Date"),
                Catalog("birth_date", "Ngày sinh", "Employee.BirthDate", "Hồ sơ nhân sự", "Date"),
                Catalog("current_contract_number", "Số hợp đồng hiện tại", "Contract.Current.ContractNumber", "Hợp đồng"),
                Catalog("contract_basic_salary", "Lương cơ bản HĐ hiện tại", "Contract.Current.BasicSalary", "Hợp đồng", "Money"),
                Catalog("contract_insurance_salary", "Lương bảo hiểm HĐ hiện tại", "Contract.Current.InsuranceSalary", "Hợp đồng", "Money"),
                Catalog("contract_start_date", "Ngày bắt đầu HĐ hiện tại", "Contract.Current.StartDate", "Hợp đồng", "Date"),
                Catalog("contract_end_date", "Ngày kết thúc HĐ hiện tại", "Contract.Current.EndDate", "Hợp đồng", "Date")
            };
        }

        private static DocumentFieldCatalogDto Catalog(string code, string label, string sourcePath, string module, string dataType = "Text")
        {
            return new DocumentFieldCatalogDto
            {
                Code = code,
                Label = label,
                SourcePath = sourcePath,
                Module = module,
                DataType = dataType,
                IsActive = true
            };
        }

        private static IReadOnlyCollection<DocumentTemplateConfigDto> HicasDefaultTemplates()
        {
            var selfServiceRoles = new[] { "Employee", "Manager", "HR", "Admin" };
            var hrRoles = new[] { "HR", "Admin" };
            var managerHrRoles = new[] { "Manager", "HR", "Admin" };

            return new List<DocumentTemplateConfigDto>
            {
                Template(
                    "LABOR_CONTRACT_PROBATION",
                    "Hợp đồng thử việc",
                    "Hợp đồng",
                    "HỢP ĐỒNG THỬ VIỆC",
                    "HD",
                    "Người lao động",
                    LaborContractFields(),
                    LaborContractBody("thử việc"),
                    "RECORD",
                    hrRoles),
                Template(
                    "LABOR_CONTRACT_FIXED_TERM",
                    "Hợp đồng lao động xác định thời hạn",
                    "Hợp đồng",
                    "HỢP ĐỒNG LAO ĐỘNG XÁC ĐỊNH THỜI HẠN",
                    "HD",
                    "Người lao động",
                    LaborContractFields(),
                    LaborContractBody("xác định thời hạn"),
                    "RECORD",
                    hrRoles),
                Template(
                    "LABOR_CONTRACT_INDEFINITE",
                    "Hợp đồng lao động không xác định thời hạn",
                    "Hợp đồng",
                    "HỢP ĐỒNG LAO ĐỘNG KHÔNG XÁC ĐỊNH THỜI HẠN",
                    "HD",
                    "Người lao động",
                    LaborContractFields(),
                    LaborContractBody("không xác định thời hạn"),
                    "RECORD",
                    hrRoles),
                Template(
                    "CONTRACT_ADDENDUM",
                    "Phụ lục hợp đồng lao động",
                    "Hợp đồng",
                    "PHỤ LỤC HỢP ĐỒNG LAO ĐỘNG",
                    "PL",
                    "Người lao động",
                    ContractAddendumFields(),
                    ContractAddendumBody(),
                    "RECORD",
                    hrRoles),
                Template(
                    "LEAVE_APPLICATION",
                    "Đơn xin nghỉ phép",
                    "Đơn đề nghị",
                    "ĐƠN XIN NGHỈ PHÉP",
                    "DN",
                    "Người làm đơn",
                    new[]
                    {
                        SystemField("employee_name", "Họ và tên", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh", "Employee.Position.Title"),
                        SystemField("direct_manager", "Quản lý trực tiếp", "Employee.Manager.FullName"),
                        ManualField("leave_from", "Nghỉ từ ngày", "Date", true),
                        ManualField("leave_to", "Đến ngày", "Date", true),
                        ComputedField("total_days", "Số ngày nghỉ", "Leave.TotalDays", "Number"),
                        ManualField("handover_to", "Người nhận bàn giao"),
                        ManualField("reason", "Lý do nghỉ", "Textarea", true)
                    },
                    "<p>Kính gửi: Ban Tổng giám đốc, Phòng Nhân sự và Quản lý trực tiếp.</p><p>Tôi tên là <strong>{employee_name}</strong>, mã nhân sự {employee_code}, hiện đang làm việc tại {current_department}, chức danh {current_position}, quản lý trực tiếp {direct_manager}.</p><p>Tôi kính đề nghị Công ty xem xét cho tôi nghỉ phép từ ngày {leave_from} đến ngày {leave_to}, tổng số {total_days} ngày làm việc.</p><p>Lý do nghỉ: {reason}.</p><p>Trong thời gian nghỉ, tôi bàn giao công việc liên quan cho {handover_to} và cam kết tuân thủ nội quy, quy chế nhân sự của {company_legal_name}.</p><p>Kính mong Công ty xem xét và phê duyệt.</p>",
                    "SELF",
                    selfServiceRoles),
                Template(
                    "RESIGNATION_APPLICATION",
                    "Đơn xin nghỉ việc",
                    "Đơn đề nghị",
                    "ĐƠN XIN NGHỈ VIỆC",
                    "DN",
                    "Người làm đơn",
                    new[]
                    {
                        SystemField("employee_name", "Họ và tên", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh", "Employee.Position.Title"),
                        SystemField("contract_number", "Số hợp đồng hiện tại", "Contract.Current.ContractNumber"),
                        ManualField("expected_last_working_date", "Ngày làm việc cuối dự kiến", "Date", true),
                        ManualField("handover_to", "Người nhận bàn giao"),
                        ManualField("reason", "Lý do nghỉ việc", "Textarea", true)
                    },
                    "<p>Kính gửi: Ban Tổng giám đốc và Phòng Nhân sự.</p><p>Tôi tên là <strong>{employee_name}</strong>, mã nhân sự {employee_code}, thuộc {current_department}, chức danh {current_position}, hợp đồng hiện tại số {contract_number}.</p><p>Tôi kính đề nghị Công ty xem xét chấm dứt quan hệ lao động với tôi kể từ ngày {expected_last_working_date}.</p><p>Lý do nghỉ việc: {reason}.</p><p>Tôi cam kết phối hợp bàn giao công việc, tài sản, hồ sơ và các nghĩa vụ liên quan cho {handover_to} theo quy định của {company_legal_name}.</p><p>Kính mong Ban Tổng giám đốc xem xét và chấp thuận.</p>",
                    "SELF",
                    selfServiceRoles),
                Template(
                    "OVERTIME_APPLICATION",
                    "Đơn đề nghị làm thêm giờ",
                    "Đơn đề nghị",
                    "ĐƠN ĐỀ NGHỊ LÀM THÊM GIỜ",
                    "LTG",
                    "Người đề nghị",
                    new[]
                    {
                        SystemField("employee_name", "Họ và tên", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh", "Employee.Position.Title"),
                        SystemField("direct_manager", "Quản lý trực tiếp", "Employee.Manager.FullName"),
                        ManualField("overtime_date", "Ngày làm thêm", "Date", true),
                        ManualField("start_time", "Từ giờ", "Time", true),
                        ManualField("end_time", "Đến giờ", "Time", true),
                        ManualField("estimated_hours", "Số giờ dự kiến", "Number", true),
                        ManualField("reason", "Nội dung/lý do làm thêm", "Textarea", true)
                    },
                    "<p>Kính gửi: Quản lý trực tiếp, Phòng Nhân sự và Ban Tổng giám đốc.</p><p>Tôi tên là <strong>{employee_name}</strong>, mã nhân sự {employee_code}, thuộc {current_department}, chức danh {current_position}, quản lý trực tiếp {direct_manager}.</p><p>Tôi đề nghị được làm thêm giờ vào ngày {overtime_date}, từ {start_time} đến {end_time}, tổng thời lượng dự kiến {estimated_hours} giờ.</p><p>Nội dung/lý do làm thêm: {reason}.</p><p>Tôi cam kết ghi nhận thời gian làm thêm trung thực và tuân thủ quy định về thời giờ làm việc, làm thêm giờ của {company_legal_name}.</p>",
                    "SELF",
                    selfServiceRoles),
                Template(
                    "EMPLOYMENT_CONFIRMATION",
                    "Giấy xác nhận công tác",
                    "Giấy xác nhận",
                    "GIẤY XÁC NHẬN CÔNG TÁC",
                    "XN",
                    "Đại diện Công ty",
                    new[]
                    {
                        SystemField("employee_name", "Người lao động", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban hiện tại", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh hiện tại", "Employee.Position.Title"),
                        SystemField("current_job_level", "Cấp bậc hiện tại", "Employee.JobLevel.Name"),
                        SystemField("joined_date", "Ngày vào làm", "Employee.JoinedDate", "Date"),
                        SystemField("contract_number", "Số hợp đồng hiện tại", "Contract.Current.ContractNumber"),
                        ManualField("confirmation_purpose", "Mục đích xác nhận", "Textarea", true)
                    },
                    "<p>{company_legal_name} xác nhận ông/bà <strong>{employee_name}</strong>, mã nhân sự {employee_code}, đang làm việc tại Công ty.</p><p>Đơn vị công tác: {current_department}. Chức danh: {current_position}. Cấp bậc: {current_job_level}. Ngày vào làm: {joined_date}. Hợp đồng hiện tại: {contract_number}.</p><p>Giấy xác nhận này được cấp để phục vụ mục đích: {confirmation_purpose}.</p><p>Thông tin xác nhận căn cứ trên hồ sơ nhân sự đang được lưu tại hệ thống HRM của {company_legal_name}.</p>",
                    "ALL",
                    hrRoles),
                Template(
                    "SALARY_CONFIRMATION",
                    "Giấy xác nhận thu nhập",
                    "Giấy xác nhận",
                    "GIẤY XÁC NHẬN THU NHẬP",
                    "XN",
                    "Đại diện Công ty",
                    new[]
                    {
                        SystemField("employee_name", "Người lao động", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban hiện tại", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh hiện tại", "Employee.Position.Title"),
                        SystemField("contract_number", "Số hợp đồng hiện tại", "Contract.Current.ContractNumber"),
                        SystemField("contract_basic_salary", "Lương cơ bản hợp đồng", "Contract.Current.BasicSalary", "Money"),
                        SystemField("contract_insurance_salary", "Lương bảo hiểm hợp đồng", "Contract.Current.InsuranceSalary", "Money"),
                        ManualField("income_period", "Kỳ/thời gian xác nhận", "Text", true),
                        ManualField("confirmation_purpose", "Mục đích xác nhận", "Textarea", true)
                    },
                    "<p>{company_legal_name} xác nhận ông/bà <strong>{employee_name}</strong>, mã nhân sự {employee_code}, thuộc {current_department}, chức danh {current_position}, hiện có hợp đồng lao động số {contract_number}.</p><p>Thông tin thu nhập theo hợp đồng/hồ sơ lương tại kỳ {income_period}: lương cơ bản {contract_basic_salary} VNĐ; lương đóng bảo hiểm {contract_insurance_salary} VNĐ.</p><p>Giấy xác nhận này được cấp để phục vụ mục đích: {confirmation_purpose}.</p><p>Thông tin nêu trên chỉ dùng cho mục đích xác nhận hành chính và không thay thế phiếu lương/quyết toán thu nhập cá nhân.</p>",
                    "ALL",
                    hrRoles),
                Template(
                    "RECRUITMENT_PROPOSAL",
                    "Tờ trình đề nghị tuyển dụng",
                    "Tờ trình",
                    "TỜ TRÌNH ĐỀ NGHỊ TUYỂN DỤNG",
                    "TT",
                    "Người lập tờ trình",
                    new[]
                    {
                        ManualField("department", "Đơn vị đề nghị", "Text", true),
                        ManualField("position", "Vị trí tuyển dụng", "Text", true),
                        ManualField("headcount", "Số lượng", "Number", true),
                        ManualField("expected_start_date", "Thời điểm cần nhân sự", "Date", true),
                        ManualField("budget_range", "Khoảng ngân sách"),
                        ManualField("reason", "Lý do tuyển dụng", "Textarea", true),
                        ManualField("requirements", "Yêu cầu chính", "Textarea", true)
                    },
                    "<p>Kính gửi: Ban Tổng giám đốc {company_legal_name}.</p><p>{department} kính trình nhu cầu tuyển dụng vị trí {position}, số lượng {headcount} nhân sự, thời điểm cần nhân sự từ ngày {expected_start_date}.</p><p>Lý do tuyển dụng: {reason}.</p><p>Yêu cầu chính đối với ứng viên: {requirements}.</p><p>Khoảng ngân sách dự kiến: {budget_range}.</p><p>Nhu cầu tuyển dụng được đặt trong định hướng hoạt động của Công ty về {company_business_line}. Kính đề nghị Ban Tổng giám đốc xem xét phê duyệt để Phòng Nhân sự triển khai quy trình tuyển dụng.</p>",
                    "RECORD",
                    managerHrRoles),
                Template(
                    "WORKING_MINUTES",
                    "Biên bản làm việc",
                    "Biên bản",
                    "BIÊN BẢN LÀM VIỆC",
                    "BB",
                    "Người lập biên bản",
                    new[]
                    {
                        ManualField("meeting_date", "Ngày làm việc", "Date", true),
                        ManualField("meeting_time", "Thời gian", "Time", true),
                        ManualField("location", "Địa điểm", "Text", true),
                        ManualField("participants", "Thành phần tham dự", "Textarea", true),
                        ManualField("content", "Nội dung làm việc", "Textarea", true),
                        ManualField("conclusion", "Kết luận/ý kiến thống nhất", "Textarea", true)
                    },
                    "<p>Hôm nay, vào lúc {meeting_time} ngày {meeting_date}, tại {location}, các bên tiến hành lập biên bản làm việc.</p><p><strong>Thành phần tham dự:</strong><br/>{participants}</p><p><strong>Nội dung làm việc:</strong><br/>{content}</p><p><strong>Kết luận/ý kiến thống nhất:</strong><br/>{conclusion}</p><p>Biên bản được lập để ghi nhận sự việc/nội dung làm việc tại {company_legal_name}, có giá trị làm căn cứ theo dõi và xử lý công việc nội bộ.</p>",
                    "RECORD",
                    managerHrRoles),
                Template(
                    "EQUIPMENT_HANDOVER_MINUTES",
                    "Biên bản bàn giao tài sản",
                    "Biên bản",
                    "BIÊN BẢN BÀN GIAO TÀI SẢN",
                    "BB",
                    "Người lập biên bản",
                    new[]
                    {
                        SystemField("employee_name", "Người nhận/bàn giao", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Phòng ban", "Employee.Department.DeptName"),
                        ManualField("handover_date", "Ngày bàn giao", "Date", true),
                        ManualField("handover_location", "Địa điểm bàn giao", "Text", true),
                        ManualField("giver", "Bên giao", "Text", true),
                        ManualField("receiver", "Bên nhận", "Text", true),
                        ManualField("asset_list", "Danh sách tài sản", "Textarea", true),
                        ManualField("handover_note", "Ghi chú/tình trạng", "Textarea")
                    },
                    "<p>Hôm nay, ngày {handover_date}, tại {handover_location}, {company_legal_name} lập biên bản bàn giao tài sản.</p><p><strong>Bên giao:</strong> {giver}. <strong>Bên nhận:</strong> {receiver}.</p><p>Người lao động liên quan: {employee_name}, mã nhân sự {employee_code}, thuộc {current_department}.</p><p><strong>Danh sách tài sản bàn giao:</strong><br/>{asset_list}</p><p><strong>Ghi chú/tình trạng:</strong><br/>{handover_note}</p><p>Các bên xác nhận thông tin trên là đúng thực tế và chịu trách nhiệm bảo quản, sử dụng tài sản theo quy định của Công ty.</p>",
                    "RECORD",
                    managerHrRoles),
                Template(
                    "TRANSFER_DECISION",
                    "Quyết định thuyên chuyển nội bộ",
                    "Quyết định nhân sự",
                    "QUYẾT ĐỊNH THUYÊN CHUYỂN NỘI BỘ",
                    "QD",
                    "Đại diện Công ty",
                    new[]
                    {
                        SystemField("employee_name", "Người lao động", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Đơn vị hiện tại", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh hiện tại", "Employee.Position.Title"),
                        ManualField("new_department", "Đơn vị mới", "Text", true),
                        ManualField("new_position", "Chức danh mới", "Text", true),
                        ManualField("effective_date", "Ngày hiệu lực", "Date", true),
                        ManualField("reason", "Căn cứ/lý do", "Textarea", true)
                    },
                    "<p class=\"doc-basis\">Căn cứ Bộ luật Lao động; căn cứ nội quy lao động, quy chế nhân sự và nhu cầu tổ chức vận hành của {company_legal_name}; căn cứ hồ sơ nhân sự và đề xuất điều chuyển nội bộ.</p><p><strong>Điều 1.</strong> Thuyên chuyển ông/bà {employee_name}, mã nhân sự {employee_code}, từ {current_department} - {current_position} sang {new_department} - {new_position}.</p><p><strong>Điều 2.</strong> Quyết định này có hiệu lực kể từ ngày {effective_date}. Các chế độ liên quan được thực hiện theo quy định hiện hành của Công ty và thỏa thuận lao động có liên quan.</p><p><strong>Điều 3.</strong> Người lao động, các đơn vị liên quan và Phòng Nhân sự chịu trách nhiệm thi hành Quyết định này.</p><p><strong>Căn cứ/lý do:</strong> {reason}.</p>",
                    "ALL",
                    hrRoles),
                Template(
                    "APPOINTMENT_DECISION",
                    "Quyết định bổ nhiệm",
                    "Quyết định nhân sự",
                    "QUYẾT ĐỊNH BỔ NHIỆM",
                    "QD",
                    "Đại diện Công ty",
                    new[]
                    {
                        SystemField("employee_name", "Người lao động", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Đơn vị hiện tại", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh hiện tại", "Employee.Position.Title"),
                        ManualField("appointment_title", "Chức danh bổ nhiệm", "Text", true),
                        ManualField("effective_date", "Ngày hiệu lực", "Date", true),
                        ManualField("responsibilities", "Nhiệm vụ/chức trách chính", "Textarea", true)
                    },
                    "<p class=\"doc-basis\">Căn cứ Bộ luật Lao động; căn cứ Điều lệ/quy chế hoạt động, quy chế nhân sự của {company_legal_name}; căn cứ năng lực, kinh nghiệm và nhu cầu quản trị của Công ty.</p><p><strong>Điều 1.</strong> Bổ nhiệm ông/bà {employee_name}, mã nhân sự {employee_code}, giữ chức danh {appointment_title} kể từ ngày {effective_date}.</p><p><strong>Điều 2.</strong> Ông/bà {employee_name} thực hiện chức trách, nhiệm vụ: {responsibilities}.</p><p><strong>Điều 3.</strong> Phòng Nhân sự, các đơn vị liên quan và ông/bà {employee_name} chịu trách nhiệm thi hành Quyết định này.</p>",
                    "ALL",
                    hrRoles),
                Template(
                    "DISCIPLINARY_DECISION",
                    "Quyết định kỷ luật/chấm dứt",
                    "Quyết định nhân sự",
                    "QUYẾT ĐỊNH KỶ LUẬT / CHẤM DỨT",
                    "QD",
                    "Đại diện Công ty",
                    new[]
                    {
                        SystemField("employee_name", "Người lao động", "Employee.FullName"),
                        SystemField("employee_code", "Mã nhân sự", "Employee.EmployeeCode"),
                        SystemField("current_department", "Đơn vị hiện tại", "Employee.Department.DeptName"),
                        SystemField("current_position", "Chức danh hiện tại", "Employee.Position.Title"),
                        ManualField("violation", "Hành vi/sự việc", "Textarea", true),
                        ManualField("disciplinary_form", "Hình thức xử lý", "Text", true),
                        ManualField("effective_date", "Ngày hiệu lực", "Date", true),
                        ManualField("legal_basis", "Căn cứ xử lý", "Textarea", true)
                    },
                    "<p class=\"doc-basis\">Căn cứ Bộ luật Lao động; căn cứ nội quy lao động, quy chế nhân sự của {company_legal_name}; căn cứ hồ sơ vụ việc, biên bản làm việc và ý kiến giải trình nếu có.</p><p><strong>Điều 1.</strong> Áp dụng hình thức xử lý {disciplinary_form} đối với ông/bà {employee_name}, mã nhân sự {employee_code}, thuộc {current_department}, chức danh {current_position}.</p><p><strong>Điều 2.</strong> Nội dung sự việc/hành vi làm căn cứ xử lý: {violation}.</p><p><strong>Điều 3.</strong> Quyết định có hiệu lực kể từ ngày {effective_date}. Phòng Nhân sự, các đơn vị liên quan và ông/bà {employee_name} chịu trách nhiệm thi hành Quyết định này.</p><p><strong>Căn cứ xử lý:</strong> {legal_basis}.</p>",
                    "ALL",
                    hrRoles)
            };
        }

        private static IEnumerable<DocumentTemplateFieldDto> LaborContractFields()
        {
            return new[]
            {
                ManualField("contract_number", "Số hợp đồng", "Text", true),
                ManualField("employee_name", "Người lao động", "Text", true),
                ManualField("employee_identity", "CCCD/CMND", "Text", true),
                ManualField("employee_address", "Địa chỉ cư trú", "Textarea", true),
                ManualField("work_title", "Chức danh/công việc", "Text", true),
                ManualField("work_location", "Địa điểm làm việc", "Text", true),
                ManualField("working_time", "Thời giờ làm việc", "Textarea", true),
                ManualField("contract_term", "Thời hạn hợp đồng", "Text", true),
                ManualField("base_salary", "Lương cơ bản", "Money", true),
                ManualField("salary_payment", "Hình thức và ngày trả lương", "Textarea", true),
                ManualField("allowances", "Phụ cấp/khoản bổ sung", "Textarea"),
                ManualField("bonus_policy", "Nguyên tắc thưởng/KPI", "Textarea"),
                ManualField("kpi_bonus_target", "Mức thưởng KPI tối đa", "Money"),
                ManualField("kpi_score_formula", "Cách tính điểm KPI", "Textarea"),
                ManualField("kpi_payout_formula", "Cách quy đổi điểm KPI thành tiền", "Textarea"),
                ManualField("kpi_bonus_eligibility_rule", "Điều kiện nhận/giảm thưởng KPI", "Textarea"),
                ManualField("kpi_bonus_payment_period", "Kỳ chi trả thưởng KPI", "Text"),
                ManualField("kpi_bonus_approver_role", "Người duyệt thưởng KPI", "Text"),
                ManualField("insurance_policy", "Chế độ bảo hiểm", "Textarea", true),
                ManualField("confidentiality_clause", "Điều khoản bảo mật", "Textarea", true),
                ManualField("ip_clause", "Điều khoản sở hữu trí tuệ", "Textarea", true),
                ManualField("termination_clause", "Chấm dứt hợp đồng", "Textarea", true)
            };
        }

        private static string LaborContractBody(string contractKind)
        {
            return $"<p class=\"doc-basis\">Căn cứ Bộ luật Lao động hiện hành; căn cứ nhu cầu sử dụng lao động của {{company_legal_name}} và thỏa thuận giữa các bên, hai bên thống nhất ký hợp đồng {contractKind} số {{contract_number}}.</p><p><strong>Người lao động:</strong> {{employee_name}}, CCCD/CMND {{employee_identity}}, địa chỉ cư trú {{employee_address}}.</p><p><strong>Công việc:</strong> {{work_title}} tại {{work_location}}. Thời giờ làm việc/nghỉ ngơi: {{working_time}}.</p><p><strong>Thời hạn:</strong> {{contract_term}}.</p><p><strong>Lương và chế độ:</strong> lương cơ bản {{base_salary}}; {{salary_payment}}. Phụ cấp/khoản bổ sung: {{allowances}}.</p><p><strong>Thưởng/KPI:</strong> {{bonus_policy}} Mức thưởng KPI tối đa: {{kpi_bonus_target}}. Cách tính điểm KPI: {{kpi_score_formula}}. Cách quy đổi thành tiền: {{kpi_payout_formula}}. Điều kiện nhận/giảm thưởng: {{kpi_bonus_eligibility_rule}}. Kỳ chi trả: {{kpi_bonus_payment_period}}. Người duyệt: {{kpi_bonus_approver_role}}.</p><p><strong>Bảo hiểm:</strong> {{insurance_policy}}.</p><p><strong>Bảo mật thông tin:</strong> {{confidentiality_clause}}.</p><p><strong>Sở hữu trí tuệ:</strong> {{ip_clause}}.</p><p><strong>Chấm dứt hợp đồng:</strong> {{termination_clause}}.</p><p>Hợp đồng được lập thành các bản có giá trị pháp lý như nhau; mỗi bên giữ ít nhất một bản để thực hiện.</p>";
        }

        private static IEnumerable<DocumentTemplateFieldDto> ContractAddendumFields()
        {
            return new[]
            {
                ManualField("addendum_number", "Số phụ lục", "Text", true),
                ManualField("base_contract_number", "Số hợp đồng gốc", "Text", true),
                ManualField("employee_name", "Người lao động", "Text", true),
                ManualField("addendum_type", "Loại phụ lục", "Text", true),
                ManualField("effective_date", "Ngày hiệu lực", "Date", true),
                ManualField("changed_content", "Nội dung thay đổi", "Textarea", true),
                ManualField("unchanged_terms", "Điều khoản giữ nguyên", "Textarea", true)
            };
        }

        private static string ContractAddendumBody()
        {
            return "<p class=\"doc-basis\">Căn cứ hợp đồng lao động số {base_contract_number}; căn cứ thỏa thuận giữa {company_legal_name} và người lao động, hai bên thống nhất lập phụ lục số {addendum_number}.</p><p><strong>Người lao động:</strong> {employee_name}.</p><p><strong>Loại phụ lục:</strong> {addendum_type}. <strong>Ngày hiệu lực:</strong> {effective_date}.</p><p><strong>Nội dung thay đổi:</strong><br/>{changed_content}</p><p><strong>Điều khoản giữ nguyên:</strong><br/>{unchanged_terms}</p><p>Phụ lục này là một phần không tách rời của hợp đồng lao động gốc và có giá trị kể từ ngày hiệu lực nêu trên.</p>";
        }

        private static DocumentTemplateConfigDto Template(
            string key,
            string displayName,
            string category,
            string title,
            string numberPrefix,
            string signerTitle,
            IEnumerable<DocumentTemplateFieldDto> fields,
            string bodyHtml,
            string? dataScope = null,
            IEnumerable<string>? allowedRoles = null)
        {
            return new DocumentTemplateConfigDto
            {
                TemplateKey = key,
                DisplayName = displayName,
                Category = category,
                DocumentTitle = title,
                NumberPrefix = numberPrefix,
                SignerTitle = signerTitle,
                Status = "Active",
                DataScope = dataScope ?? (category.Contains("Quyết định", StringComparison.OrdinalIgnoreCase) ? "ALL" : "SELF"),
                AllowedRoles = allowedRoles?.ToList() ?? (category.Contains("Quyết định", StringComparison.OrdinalIgnoreCase)
                    ? new List<string> { "Admin", "HR" }
                    : new List<string> { "Employee", "Manager", "HR", "Admin" }),
                HeaderHtml = DefaultHeaderHtml(),
                BodyHtml = bodyHtml,
                FooterHtml = DefaultFooterHtml(signerTitle),
                Fields = fields.ToList()
            };
        }

        private static DocumentTemplateFieldDto SystemField(string code, string label, string sourcePath, string dataType = "Text")
        {
            return new DocumentTemplateFieldDto
            {
                Code = code,
                Label = label,
                BindingType = "System",
                SourcePath = sourcePath,
                DataType = dataType,
                Required = false,
                IsActive = true
            };
        }

        private static DocumentTemplateFieldDto ManualField(string code, string label, string dataType = "Text", bool required = false)
        {
            return new DocumentTemplateFieldDto
            {
                Code = code,
                Label = label,
                BindingType = "Manual",
                DataType = dataType,
                Required = required,
                IsActive = true
            };
        }

        private static DocumentTemplateFieldDto ComputedField(string code, string label, string resolverKey, string dataType = "Text")
        {
            return new DocumentTemplateFieldDto
            {
                Code = code,
                Label = label,
                BindingType = "Computed",
                ResolverKey = resolverKey,
                DataType = dataType,
                IsActive = true
            };
        }

        private static string DefaultHeaderHtml()
        {
            return "<header class=\"doc-header\"><section class=\"doc-header-left\"><div class=\"doc-company-row\"><div class=\"doc-logo-mark\">HICAS</div><div class=\"doc-company-info\"><div><strong>{company_legal_name}</strong></div><div class=\"doc-small\">{company_english_name}</div><div class=\"doc-small\">MST: {company_tax_code}</div></div></div><div class=\"doc-meta\">Trụ sở: {company_address}<br/>Địa chỉ đăng ký: {company_registered_address}<br/>Điện thoại: {company_phone} · Email: {company_email}</div><div style=\"margin-top:18px\">Số: {document_number}</div></section><section class=\"doc-header-right\"><div>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div><div style=\"margin-top:2px;text-transform:none\">Độc lập - Tự do - Hạnh phúc</div></section></header><div style=\"margin-top:12px;text-align:right;font-style:italic\">{issued_place}, {issued_date_text}</div>";
        }

        private static string DefaultFooterHtml(string signerTitle)
        {
            return $"<div class=\"doc-signatures\"><div class=\"doc-signature\"><strong>{WebUtility.HtmlEncode(signerTitle).ToUpperInvariant()}</strong><br/><span style=\"font-style:italic\">(Ký, ghi rõ họ tên)</span><div style=\"margin-top:72px\"></div></div><div class=\"doc-signature\"><strong>ĐẠI DIỆN {CompanyLegalName}</strong><br/><span style=\"font-style:italic\">{CompanyRepresentativeTitle}<br/>(Ký, ghi rõ họ tên, đóng dấu nếu có)</span><div style=\"margin-top:72px\">{{signer_name}}</div></div></div>";
        }

        private sealed class ResolvedDocumentValues
        {
            public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<string> MissingFields { get; } = new();
            public List<string> Warnings { get; } = new();
        }
    }
}

