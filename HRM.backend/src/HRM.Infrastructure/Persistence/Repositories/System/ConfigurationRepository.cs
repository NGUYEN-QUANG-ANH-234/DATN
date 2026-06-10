using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
    {
        public ConfigurationRepository(MyDbContext context) : base(context) { }

        // --- Helper dùng chung để Upsert (Cập nhật hoặc Thêm mới) ---
        // BỔ SUNG: Tham số configGroup
        private async Task UpsertConfigAsync(string configGroup, string key, string value, string? description = null, bool? isActive = null, CancellationToken ct = default)
        {
            var config = await _dbSet.FirstOrDefaultAsync(c => c.ConfigGroup == configGroup && c.ParamKey == key, ct);

            if (config != null)
            {
                config.ParamValue = value;
                if (description != null) config.Description = description;
                if (isActive.HasValue) config.IsActive = isActive.Value;

                _dbSet.Update(config);
            }
            else
            {
                await _dbSet.AddAsync(new Configuration
                {
                    ConfigGroup = configGroup,
                    ParamKey = key,
                    ParamValue = value,
                    Description = description,
                    IsActive = isActive ?? true
                }, ct);
            }
        }

        // ==========================================
        // 1. SALARY VARIABLE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchVariableMappingsAsync(CancellationToken ct = default)
        {
            // Thay vì quét chuỗi StartsWith, chỉ cần lọc chính xác theo Group
            return await _dbSet
                .Where(c => c.ConfigGroup == "SALARY_VARIABLE")
                .OrderBy(c => c.ParamKey)
                .ToListAsync(ct);
        }

        public async Task EnsureVariableMappingsAsync(IEnumerable<(string Code, string Source, string Description)> variables, CancellationToken ct = default)
        {
            var definitions = variables
                .Where(variable => !string.IsNullOrWhiteSpace(variable.Code) && !string.IsNullOrWhiteSpace(variable.Source))
                .GroupBy(variable => variable.Code.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            await NormalizeLegacySalaryVariablesAsync(definitions, ct);

            var keys = definitions
                .Select(variable => $"SALARY_VAR_{variable.Code.Trim().ToUpperInvariant()}")
                .ToList();

            var existingKeys = await _dbSet
                .Where(c => c.ConfigGroup == "SALARY_VARIABLE" && keys.Contains(c.ParamKey))
                .Select(c => c.ParamKey)
                .ToListAsync(ct);

            var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = definitions
                .Where(variable => !existingSet.Contains($"SALARY_VAR_{variable.Code.Trim().ToUpperInvariant()}"))
                .Select(variable => new Configuration
                {
                    ConfigGroup = "SALARY_VARIABLE",
                    ParamKey = $"SALARY_VAR_{variable.Code.Trim().ToUpperInvariant()}",
                    ParamValue = variable.Source.Trim(),
                    Description = variable.Description.Trim(),
                    IsActive = true
                })
                .ToList();

            if (missing.Count > 0)
                await _dbSet.AddRangeAsync(missing, ct);
        }

        private async Task NormalizeLegacySalaryVariablesAsync(
            IReadOnlyCollection<(string Code, string Source, string Description)> definitions,
            CancellationToken ct = default)
        {
            var standardByCode = definitions
                .GroupBy(variable => variable.Code.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var legacyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["OT_HOURS"] = "OVERTIME_HOURS"
            };

            foreach (var mapping in legacyMappings)
            {
                if (!standardByCode.TryGetValue(mapping.Value, out var standard))
                    continue;

                var legacyKey = $"SALARY_VAR_{mapping.Key.Trim().ToUpperInvariant()}";
                var standardKey = $"SALARY_VAR_{mapping.Value.Trim().ToUpperInvariant()}";

                var legacyConfig = await _dbSet
                    .FirstOrDefaultAsync(c => c.ConfigGroup == "SALARY_VARIABLE" && c.ParamKey == legacyKey, ct);
                if (legacyConfig == null)
                    continue;

                var standardConfig = await _dbSet
                    .FirstOrDefaultAsync(c => c.ConfigGroup == "SALARY_VARIABLE" && c.ParamKey == standardKey, ct);

                if (standardConfig != null)
                {
                    _dbSet.Remove(legacyConfig);
                    continue;
                }

                legacyConfig.ParamKey = standardKey;
                legacyConfig.ParamValue = standard.Source.Trim();
                legacyConfig.Description = standard.Description.Trim();
                _dbSet.Update(legacyConfig);
            }
        }

        public async Task SaveMappingAsync(string code, string tablePath, string? description = null, bool isActive = true, CancellationToken ct = default)
        {
            await UpsertConfigAsync("SALARY_VARIABLE", $"SALARY_VAR_{code.ToUpper()}", tablePath, description ?? "Biến lương động", isActive, ct);
        }

        public async Task SetMappingActiveAsync(string code, bool isActive, CancellationToken ct = default)
        {
            var key = $"SALARY_VAR_{code.Trim().ToUpperInvariant()}";
            var config = await _dbSet.FirstOrDefaultAsync(c => c.ConfigGroup == "SALARY_VARIABLE" && c.ParamKey == key, ct);
            if (config == null)
                throw new ArgumentException("Không tìm thấy biến lương cần cập nhật trạng thái.");

            config.IsActive = isActive;
            _dbSet.Update(config);
        }

        // ==========================================
        // 2. SYSTEM CONFIG (SLA & Attendance)
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchSLAByModuleAsync(CancellationToken ct = default)
        {
            return await _dbSet.Where(c => c.ConfigGroup == "SLA_TIME").ToListAsync(ct);
        }

        public async Task EnsureSLAConfigsAsync(
            IEnumerable<(string Code, string Value, string Unit)> definitions,
            IEnumerable<(string LegacyCode, string CanonicalCode)> aliases,
            CancellationToken ct = default)
        {
            var normalizedDefinitions = definitions
                .Where(item => !string.IsNullOrWhiteSpace(item.Code))
                .GroupBy(item => item.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var configs = await _dbSet
                .Where(c => c.ConfigGroup == "SLA_TIME")
                .ToListAsync(ct);

            NormalizeLegacySlaConfigs(configs, aliases);

            foreach (var definition in normalizedDefinitions)
            {
                var code = definition.Code.Trim();
                var key = BuildSlaKey(code);
                var config = configs.FirstOrDefault(item =>
                    string.Equals(item.ParamKey, key, StringComparison.OrdinalIgnoreCase));

                if (config == null)
                {
                    await _dbSet.AddAsync(new Configuration
                    {
                        ConfigGroup = "SLA_TIME",
                        ParamKey = key,
                        ParamValue = definition.Value.Trim(),
                        Description = BuildSlaUnitDescription(definition.Unit),
                        IsActive = true
                    }, ct);
                    continue;
                }

                config.ParamKey = key;
                config.Description = BuildSlaUnitDescription(ResolveSlaUnit(config.Description, definition.Unit));
                if (string.IsNullOrWhiteSpace(config.ParamValue) || !int.TryParse(config.ParamValue, out _))
                    config.ParamValue = definition.Value.Trim();

                _dbSet.Update(config);
            }

            var canonicalKeys = normalizedDefinitions
                .Select(definition => BuildSlaKey(definition.Code))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var config in configs.Where(item => !canonicalKeys.Contains(item.ParamKey)))
            {
                config.IsActive = false;
                config.Description = "Quy trình SLA cũ - không còn hiển thị trong danh sách chuẩn.";
                _dbSet.Update(config);
            }
        }

        public async Task UpdateSLAConfigAsync(string moduleCode, string value, string unit, CancellationToken ct = default)
        {
            await UpsertConfigAsync("SLA_TIME", BuildSlaKey(moduleCode), value, BuildSlaUnitDescription(unit), ct: ct);
        }

        public async Task SetSLAConfigActiveAsync(string moduleCode, bool isActive, CancellationToken ct = default)
        {
            var key = BuildSlaKey(moduleCode);
            var config = await _dbSet.FirstOrDefaultAsync(c =>
                c.ConfigGroup == "SLA_TIME" &&
                c.ParamKey == key, ct);

            if (config == null)
                throw new ArgumentException("Không tìm thấy quy trình SLA cần cập nhật trạng thái.");

            config.IsActive = isActive;
            _dbSet.Update(config);
        }

        public async Task<IEnumerable<Configuration>> FetchLatestConfigAsync(CancellationToken ct = default)
        {
            // Lấy chính xác các nhóm liên quan đến cấu hình vận hành
            return await _dbSet.Where(c => c.ConfigGroup == "SLA_TIME" || c.ConfigGroup == "ATTENDANCE_PARAM").ToListAsync(ct);
        }

        public async Task SaveAttendanceParamsAsync(string configJsonValue, CancellationToken ct = default)
        {
            await UpsertConfigAsync("ATTENDANCE_PARAM", "ATTENDANCE_CONFIG", configJsonValue, "Cấu hình chấm công (JSON)", ct: ct);
        }

        public async Task<Configuration?> GetConfigByKeyAsync(string configGroup, string paramKey, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigGroup == configGroup && c.ParamKey == paramKey, ct);
        }

        public async Task SaveConfigAsync(string configGroup, string paramKey, string value, string? description = null, bool isActive = true, CancellationToken ct = default)
        {
            await UpsertConfigAsync(configGroup, paramKey, value, description, isActive, ct);
        }

        private static void NormalizeLegacySlaConfigs(
            List<Configuration> configs,
            IEnumerable<(string LegacyCode, string CanonicalCode)> aliases)
        {
            foreach (var alias in aliases)
            {
                var legacyKey = BuildSlaKey(alias.LegacyCode);
                var canonicalKey = BuildSlaKey(alias.CanonicalCode);

                var legacyConfig = configs.FirstOrDefault(item =>
                    string.Equals(item.ParamKey, legacyKey, StringComparison.OrdinalIgnoreCase));
                if (legacyConfig == null)
                    continue;

                var canonicalConfig = configs.FirstOrDefault(item =>
                    string.Equals(item.ParamKey, canonicalKey, StringComparison.OrdinalIgnoreCase));

                if (canonicalConfig == null)
                {
                    legacyConfig.ParamKey = canonicalKey;
                    legacyConfig.Description = BuildSlaUnitDescription(ResolveSlaUnit(legacyConfig.Description, "HOURS"));
                }
                else
                {
                    legacyConfig.IsActive = false;
                    legacyConfig.Description = $"Quy trình SLA cũ - đã được thay thế bởi {alias.CanonicalCode}.";
                }
            }
        }

        private static string BuildSlaKey(string moduleCode)
        {
            return $"SLA_{moduleCode.Trim()}";
        }

        private static string BuildSlaUnitDescription(string unit)
        {
            return $"Unit: {ResolveSlaUnit(null, unit)}";
        }

        private static string ResolveSlaUnit(string? description, string fallbackUnit)
        {
            var unit = description != null && description.StartsWith("Unit: ", StringComparison.OrdinalIgnoreCase)
                ? description["Unit: ".Length..].Trim()
                : fallbackUnit;

            return string.Equals(unit, "DAYS", StringComparison.OrdinalIgnoreCase) ? "DAYS" : "HOURS";
        }

        // ==========================================
        // 3. TEMPLATE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchAllTemplatesAsync(CancellationToken ct = default)
        {
            return await _dbSet.Where(c => c.ConfigGroup == "MAIL_TEMPLATE").ToListAsync(ct);
        }

        public async Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson, CancellationToken ct = default)
        {
            var key = templateKey.StartsWith("TEMPLATE_") ? templateKey : $"TEMPLATE_{templateKey.ToUpper()}";
            await UpsertConfigAsync("MAIL_TEMPLATE", key, subjectAndBodyJson, "Mẫu email/thông báo", ct: ct);
        }

        // ==========================================
        // 4. DOCUMENT EXPORT TEMPLATE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchDocumentTemplatesAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Where(c => c.ConfigGroup == "DOCUMENT_TEMPLATE")
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task SaveDocumentTemplateAsync(string templateKey, string templateJson, string? description = null, bool isActive = true, CancellationToken ct = default)
        {
            var key = NormalizeDocumentTemplateKey(templateKey);
            await UpsertConfigAsync("DOCUMENT_TEMPLATE", key, templateJson, description ?? "Cấu hình biểu mẫu/đơn từ", isActive, ct);
        }

        public async Task EnsureDocumentTemplatesAsync(
            IEnumerable<(string TemplateKey, string TemplateJson, string Description, bool IsActive)> templates,
            CancellationToken ct = default)
        {
            var definitions = templates
                .Where(template => !string.IsNullOrWhiteSpace(template.TemplateKey))
                .GroupBy(template => NormalizeDocumentTemplateKey(template.TemplateKey), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var keys = definitions
                .Select(template => NormalizeDocumentTemplateKey(template.TemplateKey))
                .ToList();

            var existingConfigs = await _dbSet
                .Where(c => c.ConfigGroup == "DOCUMENT_TEMPLATE" && keys.Contains(c.ParamKey))
                .ToListAsync(ct);

            foreach (var config in existingConfigs)
            {
                var definition = definitions.FirstOrDefault(template =>
                    string.Equals(NormalizeDocumentTemplateKey(template.TemplateKey), config.ParamKey, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(definition.TemplateKey))
                    continue;

                var canRefreshDefault =
                    string.IsNullOrWhiteSpace(config.Description) ||
                    config.Description.StartsWith("Biểu mẫu mặc định", StringComparison.OrdinalIgnoreCase);

                if (!canRefreshDefault)
                    continue;

                config.ParamValue = definition.TemplateJson;
                config.Description = definition.Description;
                config.IsActive = definition.IsActive;
                _dbSet.Update(config);
            }

            var existing = existingConfigs
                .Select(config => config.ParamKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = definitions
                .Where(template => !existing.Contains(NormalizeDocumentTemplateKey(template.TemplateKey)))
                .Select(template => new Configuration
                {
                    ConfigGroup = "DOCUMENT_TEMPLATE",
                    ParamKey = NormalizeDocumentTemplateKey(template.TemplateKey),
                    ParamValue = template.TemplateJson,
                    Description = template.Description,
                    IsActive = template.IsActive
                })
                .ToList();

            if (missing.Count > 0)
                await _dbSet.AddRangeAsync(missing, ct);
        }

        private static string NormalizeDocumentTemplateKey(string templateKey)
        {
            return templateKey.Trim().ToUpperInvariant();
        }
    }
}
