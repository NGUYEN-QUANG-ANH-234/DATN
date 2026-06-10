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

        public NotificationTemplateRenderer(
            IConfigurationRepository configRepo,
            IAppCache cache,
            ILockService lockService)
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
            var normalizedKey = templateKey.ToUpperInvariant().Replace("TEMPLATE_", "");
            var template = await GetTemplateAsync(normalizedKey, ct);

            return (
                ReplaceTokens(template.Subject, tokens),
                ReplaceTokens(template.BodyHtml, tokens));
        }

        private async Task<TemplateDto> GetTemplateAsync(string templateKey, CancellationToken ct)
        {
            var cacheKey = $"{CacheKeyPrefix}{templateKey}";
            return await _cache.GetOrSetWithLockAsync(
                cacheKey,
                async innerCt =>
                {
                    var configs = await _configRepo.FetchAllTemplatesAsync(innerCt);
                    var config = configs.FirstOrDefault(item =>
                        string.Equals(item.ParamKey, $"TEMPLATE_{templateKey}", StringComparison.OrdinalIgnoreCase));

                    if (config == null)
                        return BuildFallbackTemplate(templateKey);

                    var content = JsonSerializer.Deserialize<TemplateContent>(config.ParamValue);
                    if (content == null)
                        return BuildFallbackTemplate(templateKey);

                    return new TemplateDto
                    {
                        TemplateKey = templateKey,
                        Subject = content.Subject,
                        BodyHtml = content.BodyHtml
                    };
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        private static TemplateDto BuildFallbackTemplate(string templateKey)
        {
            return new TemplateDto
            {
                TemplateKey = templateKey,
                Subject = templateKey,
                BodyHtml = $"Mau {templateKey} chua duoc cau hinh."
            };
        }

        private static string ReplaceTokens(string content, IDictionary<string, string> tokens)
        {
            return tokens.Aggregate(content, (current, token) =>
                current.Replace($"{{{token.Key}}}", token.Value, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record TemplateContent(
            string Subject,
            string BodyHtml);
    }
}
