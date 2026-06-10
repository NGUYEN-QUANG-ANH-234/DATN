using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class TemplateManagementUseCase : ITemplateManagementUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "Notification_Template_Cache_v4";

        private static readonly List<NotificationTemplateDefinition> DefaultTemplates = new()
        {
            Template("PROMOTION", "Thông báo thăng tiến", "Biến động nhân sự", new[] { "{name}", "{position}", "{date}" },
                "Thông báo quyết định thăng tiến",
                "<p>Xin chào {name},</p><p>Bạn đã được ghi nhận thay đổi vị trí/chức danh mới: <b>{position}</b>, hiệu lực từ ngày <b>{date}</b>.</p><p>Vui lòng theo dõi hồ sơ nhân sự để nhận quyết định chính thức.</p>"),
            Template("NEW_TASK", "Giao việc mới", "Công việc và đào tạo", new[] { "{name}", "{task_name}", "{deadline}" },
                "Bạn được giao công việc mới: {task_name}",
                "<p>Xin chào {name},</p><p>Bạn vừa được giao công việc <b>{task_name}</b>. Hạn hoàn thành: <b>{deadline}</b>.</p><p>Vui lòng cập nhật tiến độ đúng thời hạn.</p>"),
            Template("SLA_WARNING", "Cảnh báo SLA", "Hệ thống", new[] { "{name}", "{module}", "{hours_left}" },
                "Cảnh báo SLA sắp quá hạn: {module}",
                "<p>Xin chào {name},</p><p>Quy trình <b>{module}</b> còn <b>{hours_left}</b> giờ trước khi quá hạn SLA.</p><p>Vui lòng xử lý hoặc chuyển cấp theo đúng quy trình.</p>"),
            Template("LEAVE_REQUEST_CREATED", "Tạo đơn nghỉ phép", "Chấm công và nghỉ phép", new[] { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}" },
                "Đơn nghỉ phép mới từ {name}",
                "<p>Nhân viên <b>{name}</b> vừa tạo đơn <b>{leave_type}</b> từ <b>{start_date}</b> đến <b>{end_date}</b>, tổng <b>{days}</b> ngày.</p><p>Trạng thái hiện tại: <b>{status}</b>.</p>"),
            Template("LEAVE_REQUEST_APPROVED", "Duyệt đơn nghỉ phép", "Chấm công và nghỉ phép", new[] { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}" },
                "Đơn nghỉ phép của {name} đã được duyệt",
                "<p>Đơn <b>{leave_type}</b> của <b>{name}</b> từ <b>{start_date}</b> đến <b>{end_date}</b> đã được duyệt.</p><p>Tổng số ngày: <b>{days}</b>. Trạng thái: <b>{status}</b>.</p>"),
            Template("LEAVE_REQUEST_REJECTED", "Từ chối đơn nghỉ phép", "Chấm công và nghỉ phép", new[] { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}", "{reason}" },
                "Đơn nghỉ phép của {name} bị từ chối",
                "<p>Đơn <b>{leave_type}</b> của <b>{name}</b> từ <b>{start_date}</b> đến <b>{end_date}</b> đã bị từ chối.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("RECRUITMENT_REQUEST_SUBMITTED", "Tạo nhu cầu tuyển dụng", "Tuyển dụng", new[] { "{name}", "{department}", "{position}", "{quantity}", "{deadline}" },
                "Nhu cầu tuyển dụng mới: {position}",
                "<p><b>{name}</b> vừa tạo nhu cầu tuyển dụng vị trí <b>{position}</b> cho phòng ban <b>{department}</b>.</p><p>Số lượng: <b>{quantity}</b>. Hạn cần nhân sự: <b>{deadline}</b>.</p>"),
            Template("RECRUITMENT_APPROVED", "Duyệt nhu cầu tuyển dụng", "Tuyển dụng", new[] { "{name}", "{department}", "{position}", "{status}" },
                "Nhu cầu tuyển dụng đã được duyệt",
                "<p>Nhu cầu tuyển dụng vị trí <b>{position}</b> của phòng ban <b>{department}</b> đã được duyệt.</p><p>Người tạo: <b>{name}</b>. Trạng thái: <b>{status}</b>.</p>"),
            Template("CANDIDATE_APPROVAL_REQUIRED", "Yêu cầu duyệt ứng viên", "Tuyển dụng", new[] { "{candidate_name}", "{position}", "{stage}", "{deadline}" },
                "Cần duyệt ứng viên {candidate_name}",
                "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đang chờ duyệt ở bước <b>{stage}</b>.</p><p>Hạn xử lý: <b>{deadline}</b>.</p>"),
            Template("CANDIDATE_APPROVED", "Ứng viên được duyệt", "Tuyển dụng", new[] { "{candidate_name}", "{position}", "{status}" },
                "Ứng viên {candidate_name} đã được duyệt",
                "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đã được duyệt.</p><p>Trạng thái: <b>{status}</b>.</p>"),
            Template("CANDIDATE_REJECTED", "Ứng viên bị từ chối", "Tuyển dụng", new[] { "{candidate_name}", "{position}", "{reason}" },
                "Ứng viên {candidate_name} bị từ chối",
                "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đã bị từ chối.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("ONBOARDING_REQUEST_CREATED", "Tạo hồ sơ onboarding", "Hồ sơ và hợp đồng", new[] { "{candidate_name}", "{employee_name}", "{department}", "{position}" },
                "Hồ sơ onboarding mới: {employee_name}",
                "<p>Hệ thống vừa ghi nhận hồ sơ onboarding cho <b>{employee_name}</b> từ ứng viên <b>{candidate_name}</b>.</p><p>Phòng ban: <b>{department}</b>. Vị trí: <b>{position}</b>.</p>"),
            Template("PROFILE_UPDATE_SUBMITTED", "Nhân viên yêu cầu cập nhật hồ sơ", "Hồ sơ và hợp đồng", new[] { "{name}", "{fields}", "{submitted_at}" },
                "Yêu cầu cập nhật hồ sơ từ {name}",
                "<p>Nhân viên <b>{name}</b> vừa gửi yêu cầu cập nhật các trường: <b>{fields}</b>.</p><p>Thời điểm gửi: <b>{submitted_at}</b>.</p>"),
            Template("PROFILE_UPDATE_APPROVED", "Duyệt cập nhật hồ sơ", "Hồ sơ và hợp đồng", new[] { "{name}", "{fields}", "{status}" },
                "Cập nhật hồ sơ của {name} đã được duyệt",
                "<p>Yêu cầu cập nhật hồ sơ của <b>{name}</b> đã được duyệt.</p><p>Các trường cập nhật: <b>{fields}</b>. Trạng thái: <b>{status}</b>.</p>"),
            Template("PROFILE_UPDATE_REJECTED", "Từ chối cập nhật hồ sơ", "Hồ sơ và hợp đồng", new[] { "{name}", "{fields}", "{reason}" },
                "Cập nhật hồ sơ của {name} bị từ chối",
                "<p>Yêu cầu cập nhật hồ sơ của <b>{name}</b> đã bị từ chối.</p><p>Các trường: <b>{fields}</b>. Lý do: <b>{reason}</b>.</p>"),
            Template("CONTRACT_FLOW_REQUIRED", "Yêu cầu xử lý hợp đồng", "Hồ sơ và hợp đồng", new[] { "{name}", "{contract_type}", "{deadline}" },
                "Cần xử lý hợp đồng cho {name}",
                "<p>Hồ sơ hợp đồng của <b>{name}</b> cần được xử lý.</p><p>Loại hợp đồng/phụ lục: <b>{contract_type}</b>. Hạn xử lý: <b>{deadline}</b>.</p>"),
            Template("CONTRACT_SIGNED", "Hợp đồng đã ký", "Hồ sơ và hợp đồng", new[] { "{name}", "{contract_number}", "{effective_date}" },
                "Hợp đồng {contract_number} đã hoàn tất",
                "<p>Hợp đồng <b>{contract_number}</b> của <b>{name}</b> đã hoàn tất ký/chấp thuận.</p><p>Ngày hiệu lực: <b>{effective_date}</b>.</p>"),
            Template("CONTRACT_REJECTED", "Hợp đồng bị từ chối", "Hồ sơ và hợp đồng", new[] { "{name}", "{contract_number}", "{reason}" },
                "Hợp đồng {contract_number} bị từ chối",
                "<p>Hợp đồng <b>{contract_number}</b> của <b>{name}</b> bị từ chối.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("OVERTIME_REQUEST_CREATED", "Tạo yêu cầu tăng ca", "Chấm công và nghỉ phép", new[] { "{name}", "{work_date}", "{start_time}", "{end_time}", "{reason}" },
                "Yêu cầu tăng ca mới từ {name}",
                "<p><b>{name}</b> vừa tạo yêu cầu tăng ca ngày <b>{work_date}</b>, từ <b>{start_time}</b> đến <b>{end_time}</b>.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("OVERTIME_APPROVED", "Duyệt tăng ca", "Chấm công và nghỉ phép", new[] { "{name}", "{work_date}", "{approved_minutes}", "{status}" },
                "Yêu cầu tăng ca của {name} đã được duyệt",
                "<p>Yêu cầu tăng ca ngày <b>{work_date}</b> của <b>{name}</b> đã được duyệt.</p><p>Số phút được duyệt: <b>{approved_minutes}</b>. Trạng thái: <b>{status}</b>.</p>"),
            Template("OVERTIME_REJECTED", "Từ chối tăng ca", "Chấm công và nghỉ phép", new[] { "{name}", "{work_date}", "{reason}" },
                "Yêu cầu tăng ca của {name} bị từ chối",
                "<p>Yêu cầu tăng ca ngày <b>{work_date}</b> của <b>{name}</b> đã bị từ chối.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("PAYROLL_PUBLISHED", "Công bố bảng lương", "Lương", new[] { "{name}", "{period}", "{net_salary}", "{status}" },
                "Bảng lương kỳ {period} đã được công bố",
                "<p>Xin chào {name},</p><p>Bảng lương kỳ <b>{period}</b> đã được công bố. Lương thực nhận: <b>{net_salary}</b>. Trạng thái: <b>{status}</b>.</p>"),
            Template("PAYROLL_ADJUSTMENT_APPROVED", "Duyệt điều chỉnh lương", "Lương", new[] { "{name}", "{period}", "{amount}", "{reason}" },
                "Điều chỉnh lương kỳ {period} đã được duyệt",
                "<p>Điều chỉnh lương của <b>{name}</b> trong kỳ <b>{period}</b> đã được duyệt.</p><p>Số tiền: <b>{amount}</b>. Lý do: <b>{reason}</b>.</p>"),
            Template("KPI_REVIEW_CREATED", "Tạo đánh giá KPI", "Hiệu suất và đào tạo", new[] { "{name}", "{period}", "{deadline}" },
                "Phiếu đánh giá KPI kỳ {period}",
                "<p>Phiếu đánh giá KPI kỳ <b>{period}</b> của <b>{name}</b> đã được tạo.</p><p>Hạn xử lý: <b>{deadline}</b>.</p>"),
            Template("TRAINING_EVALUATION_DUE", "Nhắc đánh giá đào tạo", "Hiệu suất và đào tạo", new[] { "{name}", "{course_name}", "{deadline}" },
                "Cần đánh giá đào tạo: {course_name}",
                "<p>Khóa đào tạo <b>{course_name}</b> của <b>{name}</b> đang chờ đánh giá.</p><p>Hạn xử lý: <b>{deadline}</b>.</p>"),
            Template("PENALTY_CREATED", "Ghi nhận vi phạm", "Hiệu suất và đào tạo", new[] { "{name}", "{rule_code}", "{point}", "{reason}" },
                "Ghi nhận vi phạm: {rule_code}",
                "<p>Hệ thống ghi nhận vi phạm cho <b>{name}</b>.</p><p>Mã luật: <b>{rule_code}</b>. Điểm trừ: <b>{point}</b>. Lý do: <b>{reason}</b>.</p>"),
            Template("PERSONNEL_CHANGE_CREATED", "Tạo hồ sơ biến động nhân sự", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{effective_date}", "{reason}" },
                "Hồ sơ biến động nhân sự mới: {change_type}",
                "<p>Hồ sơ biến động nhân sự của <b>{name}</b> vừa được tạo.</p><p>Loại biến động: <b>{change_type}</b>. Ngày hiệu lực dự kiến: <b>{effective_date}</b>. Lý do: <b>{reason}</b>.</p>"),
            Template("PERSONNEL_CHANGE_EMPLOYEE_CONSENT", "Yêu cầu nhân viên phản hồi biến động", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{deadline}" },
                "Cần phản hồi hồ sơ biến động nhân sự",
                "<p>Xin chào {name},</p><p>Hồ sơ <b>{change_type}</b> cần phản hồi từ bạn trước <b>{deadline}</b>.</p>"),
            Template("PERSONNEL_CHANGE_DIRECTOR_APPROVAL", "Yêu cầu giám đốc duyệt biến động", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{effective_date}" },
                "Cần duyệt biến động nhân sự: {change_type}",
                "<p>Hồ sơ biến động nhân sự của <b>{name}</b> đang chờ duyệt.</p><p>Loại: <b>{change_type}</b>. Ngày hiệu lực: <b>{effective_date}</b>.</p>"),
            Template("PERSONNEL_CHANGE_APPROVED", "Biến động nhân sự được duyệt", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{effective_date}" },
                "Hồ sơ biến động nhân sự đã được duyệt",
                "<p>Hồ sơ <b>{change_type}</b> của <b>{name}</b> đã được duyệt.</p><p>Ngày hiệu lực: <b>{effective_date}</b>.</p>"),
            Template("PERSONNEL_CHANGE_REJECTED", "Biến động nhân sự bị từ chối", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{reason}" },
                "Hồ sơ biến động nhân sự bị từ chối",
                "<p>Hồ sơ <b>{change_type}</b> của <b>{name}</b> đã bị từ chối.</p><p>Lý do: <b>{reason}</b>.</p>"),
            Template("PERSONNEL_CHANGE_EXECUTED", "Biến động nhân sự đã thực thi", "Biến động nhân sự", new[] { "{name}", "{change_type}", "{effective_date}" },
                "Đã thực thi biến động nhân sự",
                "<p>Biến động nhân sự <b>{change_type}</b> của <b>{name}</b> đã hoàn tất thực thi.</p><p>Ngày hiệu lực: <b>{effective_date}</b>.</p>")
        };

        public TemplateManagementUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync("notification_template_default_seed", async innerCt =>
            {
                await SeedDefaultTemplatesAsync(innerCt);
                return true;
            }, cancellationToken: ct);

            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async innerCt =>
                {
                    var configs = await _configRepo.FetchAllTemplatesAsync(innerCt);
                    var configsByKey = configs
                        .GroupBy(config => NormalizeKey(config.ParamKey), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                    var templates = new List<TemplateDto>();

                    foreach (var definition in DefaultTemplates)
                    {
                        configsByKey.TryGetValue(definition.Key, out var config);
                        var content = TryReadTemplateContent(config?.ParamValue);

                        templates.Add(EnrichTemplate(definition.Key, new TemplateDto
                        {
                            TemplateKey = definition.Key,
                            Subject = content?.Subject ?? definition.Subject,
                            BodyHtml = content?.BodyHtml ?? definition.BodyHtml,
                            CustomVariables = content?.CustomVariables ?? new List<TemplateVariableDto>()
                        }));
                    }

                    var defaultKeys = DefaultTemplates
                        .Select(template => template.Key)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var config in configsByKey.Values.Where(config =>
                                 !defaultKeys.Contains(NormalizeKey(config.ParamKey))))
                    {
                        var rawKey = NormalizeKey(config.ParamKey);
                        var content = TryReadTemplateContent(config.ParamValue);
                        if (content == null)
                            continue;

                        templates.Add(EnrichTemplate(rawKey, new TemplateDto
                        {
                            TemplateKey = rawKey,
                            Subject = content.Subject,
                            BodyHtml = content.BodyHtml,
                            CustomVariables = content.CustomVariables ?? new List<TemplateVariableDto>()
                        }));
                    }

                    return templates
                        .OrderBy(template => template.Category)
                        .ThenBy(template => template.DisplayName)
                        .ThenBy(template => template.TemplateKey)
                        .ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> UpdateTemplateAsync(TemplateDto dto, int adminId, CancellationToken ct = default)
        {
            var rawKey = NormalizeKey(dto.TemplateKey);
            var definition = FindDefinition(rawKey);
            var systemPlaceholders = definition?.Placeholders ?? Array.Empty<string>();
            var customVariables = NormalizeCustomVariables(dto.CustomVariables, systemPlaceholders, rawKey);
            var allowedPlaceholders = systemPlaceholders
                .Concat(customVariables.Select(variable => variable.Placeholder))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ValidatePlaceholders(rawKey, $"{dto.Subject}\n{dto.BodyHtml}", allowedPlaceholders);

            var isSuccess = false;

            await _lockService.GetWithLockAsync($"notification_template_{rawKey}", async innerCt =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var jsonContent = JsonSerializer.Serialize(new TemplateContent(dto.Subject, dto.BodyHtml, customVariables));
                    await _configRepo.UpdateTemplateContentAsync(rawKey, jsonContent, innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, ct);
                await _cache.RemoveAsync($"Notification_Template_Render_{rawKey}", ct);
            }

            return isSuccess;
        }

        private async Task SeedDefaultTemplatesAsync(CancellationToken ct)
        {
            var configs = await _configRepo.FetchAllTemplatesAsync(ct);
            var existingKeys = configs
                .Select(config => config.ParamKey.Replace("TEMPLATE_", ""))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var template in DefaultTemplates.Where(template => !existingKeys.Contains(template.Key)))
            {
                var jsonContent = JsonSerializer.Serialize(new TemplateContent(template.Subject, template.BodyHtml));

                await _configRepo.UpdateTemplateContentAsync(template.Key, jsonContent, ct);
            }

            await _unitOfWork.CommitAsync(ct);
        }

        private static TemplateDto EnrichTemplate(string rawKey, TemplateDto dto)
        {
            var key = NormalizeKey(rawKey);
            var definition = FindDefinition(key);
            var systemPlaceholders = definition?.Placeholders.ToList() ?? new List<string>();
            var customVariables = NormalizeCustomVariablesForRead(dto.CustomVariables, systemPlaceholders);

            dto.TemplateKey = key;
            dto.DisplayName = definition?.DisplayName ?? key;
            dto.Category = definition?.Category ?? "Mẫu tùy chỉnh";
            dto.SystemPlaceholders = systemPlaceholders;
            dto.CustomVariables = customVariables;
            dto.AllowedPlaceholders = systemPlaceholders
                .Concat(customVariables.Select(variable => variable.Placeholder))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(placeholder => placeholder)
                .ToList();

            if (dto.AllowedPlaceholders.Count == 0)
                dto.AllowedPlaceholders = ExtractPlaceholders($"{dto.Subject}\n{dto.BodyHtml}");
            return dto;
        }

        private static void ValidatePlaceholders(string rawKey, string bodyHtml, IReadOnlyCollection<string> allowedVars)
        {
            var matches = Regex.Matches(bodyHtml, @"\{[a-z][a-z0-9_]*\}");
            foreach (Match match in matches)
            {
                if (!allowedVars.Contains(match.Value, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException($"Biến '{match.Value}' không hợp lệ cho mẫu {rawKey}. Các biến cho phép: {string.Join(", ", allowedVars)}");
            }
        }

        private static List<string> ExtractPlaceholders(string bodyHtml)
        {
            return Regex.Matches(bodyHtml, @"\{[a-z][a-z0-9_]*\}")
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();
        }

        private static List<TemplateVariableDto> NormalizeCustomVariables(
            IEnumerable<TemplateVariableDto>? variables,
            IReadOnlyCollection<string> reservedPlaceholders,
            string templateKey)
        {
            var normalized = NormalizeCustomVariablesForRead(variables, reservedPlaceholders);
            var duplicateCodes = normalized
                .GroupBy(variable => variable.Code, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateCodes.Count > 0)
                throw new ArgumentException($"Mau {templateKey} co bien bi trung: {string.Join(", ", duplicateCodes)}.");

            return normalized;
        }

        private static List<TemplateVariableDto> NormalizeCustomVariablesForRead(
            IEnumerable<TemplateVariableDto>? variables,
            IReadOnlyCollection<string> reservedPlaceholders)
        {
            var allowedDataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Text", "Textarea", "Number", "Money", "Date", "DateTime", "Boolean"
            };

            var reserved = reservedPlaceholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<TemplateVariableDto>();

            foreach (var variable in variables ?? Enumerable.Empty<TemplateVariableDto>())
            {
                var code = (variable.Code ?? string.Empty).Trim().ToLowerInvariant();
                if (!Regex.IsMatch(code, @"^[a-z][a-z0-9_]{1,49}$"))
                    continue;

                var placeholder = $"{{{code}}}";
                if (reserved.Contains(placeholder))
                    continue;

                var dataType = string.IsNullOrWhiteSpace(variable.DataType)
                    ? "Text"
                    : variable.DataType.Trim();
                if (!allowedDataTypes.Contains(dataType))
                    dataType = "Text";

                result.Add(new TemplateVariableDto
                {
                    Code = code,
                    Label = string.IsNullOrWhiteSpace(variable.Label) ? code : variable.Label.Trim(),
                    DataType = dataType,
                    SourceType = "Manual",
                    IsRequired = variable.IsRequired,
                    Description = string.IsNullOrWhiteSpace(variable.Description)
                        ? null
                        : variable.Description.Trim()
                });
            }

            return result;
        }

        private static TemplateContent? TryReadTemplateContent(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TemplateContent>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeKey(string key)
        {
            return key.Trim().ToUpperInvariant().Replace("TEMPLATE_", "");
        }

        private static NotificationTemplateDefinition? FindDefinition(string key)
        {
            return DefaultTemplates.FirstOrDefault(template =>
                string.Equals(template.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static NotificationTemplateDefinition Template(
            string key,
            string displayName,
            string category,
            IEnumerable<string> placeholders,
            string subject,
            string bodyHtml)
        {
            return new NotificationTemplateDefinition(
                key,
                displayName,
                category,
                placeholders.ToList(),
                subject,
                bodyHtml);
        }

        private sealed record NotificationTemplateDefinition(
            string Key,
            string DisplayName,
            string Category,
            IReadOnlyCollection<string> Placeholders,
            string Subject,
            string BodyHtml);

        private sealed record TemplateContent(
            string Subject,
            string BodyHtml,
            List<TemplateVariableDto>? CustomVariables = null);
    }
}
