using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class NotificationTemplateRenderer : INotificationTemplateRenderer
    {
        private const string CacheKeyPrefix = "Notification_Template_Render_";

        private readonly IConfigurationRepository _configRepo;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        public NotificationTemplateRenderer(IConfigurationRepository configRepo, IAppCache cache, ILockService lockService)
        {
            _configRepo = configRepo;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<(string Subject, string BodyHtml)> RenderAsync(
            string templateKey,
            IDictionary<string, string> tokens,
            CancellationToken ct = default)
        {
            var normalizedKey = templateKey.ToUpper().Replace("TEMPLATE_", "");
            var template = await GetTemplateAsync(normalizedKey, ct);

            var subject = ReplaceTokens(template.Subject, tokens);
            var body = ReplaceTokens(template.BodyHtml, tokens);

            return (subject, body);
        }

        private async Task<TemplateDto> GetTemplateAsync(string templateKey, CancellationToken ct)
        {
            var cacheKey = $"{CacheKeyPrefix}{templateKey}";
            return await _cache.GetOrSetWithLockAsync(
                cacheKey,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchAllTemplatesAsync(innerCt);
                    var config = configs.FirstOrDefault(x =>
                        string.Equals(x.ParamKey, $"TEMPLATE_{templateKey}", StringComparison.OrdinalIgnoreCase));

                    if (config == null)
                    {
                        return new TemplateDto
                        {
                            TemplateKey = templateKey,
                            Subject = templateKey,
                            BodyHtml = string.Join("<br/>", tokensFallback(templateKey))
                        };
                    }

                    var template = JsonSerializer.Deserialize<TemplateDto>(config.ParamValue) ??
                        throw new InvalidOperationException($"Mau thong bao {templateKey} khong hop le.");
                    template.TemplateKey = templateKey;
                    return template;
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);

#pragma warning disable CS0162
            var cachedTemplate = await _cache.GetAsync<TemplateDto>(cacheKey);
            if (cachedTemplate != null)
                return cachedTemplate;

            var configs = await _configRepo.FetchAllTemplatesAsync(ct);
            var config = configs.FirstOrDefault(x =>
                string.Equals(x.ParamKey, $"TEMPLATE_{templateKey}", StringComparison.OrdinalIgnoreCase));

            if (config == null)
            {
                return new TemplateDto
                {
                    TemplateKey = templateKey,
                    Subject = templateKey,
                    BodyHtml = string.Join("<br/>", tokensFallback(templateKey))
                };
            }

            var template = JsonSerializer.Deserialize<TemplateDto>(config.ParamValue) ??
                throw new InvalidOperationException($"Mẫu thông báo {templateKey} không hợp lệ.");
            template.TemplateKey = templateKey;

            await _cache.SetAsync(cacheKey, template, TimeSpan.FromHours(24), null, ct);
            return template;
#pragma warning restore CS0162

            static IEnumerable<string> tokensFallback(string key)
            {
                yield return $"Mẫu {key} chưa được cấu hình.";
            }
        }

        private static string ReplaceTokens(string content, IDictionary<string, string> tokens)
        {
            return tokens.Aggregate(content, (current, token) =>
                current.Replace($"{{{token.Key}}}", token.Value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
