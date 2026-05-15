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

        private const string CACHE_KEY = "Notification_Template_Cache";

        private readonly Dictionary<string, List<string>> _validPlaceholders = new()
        {
            { "PROMOTION", new List<string> { "{name}", "{position}", "{date}" } },
            { "NEW_TASK", new List<string> { "{name}", "{task_name}", "{deadline}" } },
            { "SLA_WARNING", new List<string> { "{name}", "{module}", "{hours_left}" } }
        };

        public TemplateManagementUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
        {
            var cachedTemplates = await _cache.GetAsync<IEnumerable<TemplateDto>>(CACHE_KEY);
            if (cachedTemplates != null) return cachedTemplates;

            var configs = await _configRepo.FetchAllTemplatesAsync(ct);
            var templates = new List<TemplateDto>();

            foreach (var c in configs)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<TemplateDto>(c.ParamValue);
                    if (parsed != null)
                    {
                        parsed.TemplateKey = c.ParamKey.Replace("TEMPLATE_", "");
                        templates.Add(parsed);
                    }
                }
                catch
                {
                    // Bỏ qua lỗi Deserialize
                }
            }

            await _cache.SetAsync(CACHE_KEY, templates, TimeSpan.FromHours(24), null, ct);
            return templates;
        }

        public async Task<bool> UpdateTemplateAsync(TemplateDto dto, int adminId, CancellationToken ct = default)
        {
            var rawKey = dto.TemplateKey.ToUpper().Replace("TEMPLATE_", "");
            if (_validPlaceholders.TryGetValue(rawKey, out var allowedVars))
            {
                var matches = Regex.Matches(dto.BodyHtml, @"\{[a-z_]+\}");
                foreach (Match match in matches)
                {
                    if (!allowedVars.Contains(match.Value))
                        throw new ArgumentException($"Biến '{match.Value}' không hợp lệ cho mẫu {rawKey}. Các biến cho phép: {string.Join(", ", allowedVars)}");
                }
            }

            bool isSuccess = false;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var jsonContent = JsonSerializer.Serialize(new { dto.Subject, dto.BodyHtml });
                await _configRepo.UpdateTemplateContentAsync(rawKey, jsonContent, ct);

                // Ghi log đã được chuyển giao cho DbContext Hook lo liệu

                await _unitOfWork.CommitAsync(ct);
                isSuccess = true;
            }, ct);

            if (isSuccess) await _cache.RemoveAsync(CACHE_KEY, ct);

            return isSuccess;
        }
    }
}