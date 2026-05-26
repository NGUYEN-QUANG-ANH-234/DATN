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

        private const string CACHE_KEY = "Notification_Template_Cache";

        private readonly Dictionary<string, List<string>> _validPlaceholders = new()
        {
            { "PROMOTION", new List<string> { "{name}", "{position}", "{date}" } },
            { "NEW_TASK", new List<string> { "{name}", "{task_name}", "{deadline}" } },
            { "SLA_WARNING", new List<string> { "{name}", "{module}", "{hours_left}" } },
            { "LEAVE_REQUEST_CREATED", new List<string> { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}" } },
            { "LEAVE_REQUEST_APPROVED", new List<string> { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}" } },
            { "LEAVE_REQUEST_REJECTED", new List<string> { "{name}", "{leave_type}", "{start_date}", "{end_date}", "{days}", "{status}", "{reason}" } }
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
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchAllTemplatesAsync(innerCt);
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

                    return templates;
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
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

            await _lockService.GetWithLockAsync($"notification_template_{rawKey}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var jsonContent = JsonSerializer.Serialize(new { dto.Subject, dto.BodyHtml });
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
    }
}
