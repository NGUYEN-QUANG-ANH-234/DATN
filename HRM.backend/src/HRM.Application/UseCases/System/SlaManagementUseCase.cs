using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SlaManagementUseCase : ISlaManagementUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;
        private readonly ISlaProcessRegistry _slaProcessRegistry;

        private const string CACHE_KEY = "SLA_Config_Cache_v2";

        public SlaManagementUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService,
            ISlaProcessRegistry slaProcessRegistry)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
            _slaProcessRegistry = slaProcessRegistry;
        }

        public async Task<IEnumerable<SlaDto>> GetSLAConfigsAsync(CancellationToken ct = default)
        {
            await EnsureSeededAsync(ct);

            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var definitions = _slaProcessRegistry.GetProcesses();
                    var configs = await _configRepo.FetchSLAByModuleAsync(innerCt);

                    return definitions
                        .OrderBy(definition => definition.ModuleName)
                        .ThenBy(definition => definition.DisplayName)
                        .Select(definition => BuildDto(definition, configs))
                        .ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> UpdateSLAParameterAsync(SlaDto dto, int adminId, CancellationToken ct = default)
        {
            var canonicalCode = _slaProcessRegistry.ResolveCanonicalCode(dto.ModuleCode);
            if (canonicalCode == null)
                throw new ArgumentException("Quy trình SLA không nằm trong danh sách hệ thống cho phép.");

            if (!int.TryParse(dto.Value, out int timeValue) || timeValue <= 0)
                throw new ArgumentException("Thời gian SLA phải là một số nguyên dương (> 0).");

            if (dto.Unit.ToUpper() != "HOURS" && dto.Unit.ToUpper() != "DAYS")
                throw new ArgumentException("Đơn vị thời gian không hợp lệ. Chỉ chấp nhận 'HOURS' hoặc 'DAYS'.");

            var isSuccess = false;

            await _lockService.GetWithLockAsync($"sla_config_{canonicalCode}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    await EnsureSeededAsync(innerCt, commit: false);
                    await _configRepo.UpdateSLAConfigAsync(canonicalCode, dto.Value, dto.Unit.ToUpper(), innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess)
                await _cache.RemoveAsync(CACHE_KEY, ct);

            return isSuccess;
        }

        public async Task<bool> SetSLAActiveAsync(string moduleCode, bool isActive, int adminId, CancellationToken ct = default)
        {
            var canonicalCode = _slaProcessRegistry.ResolveCanonicalCode(moduleCode);
            if (canonicalCode == null)
                throw new ArgumentException("Quy trình SLA không nằm trong danh sách hệ thống cho phép.");

            var isSuccess = false;

            await _lockService.GetWithLockAsync($"sla_config_active_{canonicalCode}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    await EnsureSeededAsync(innerCt, commit: false);
                    await _configRepo.SetSLAConfigActiveAsync(canonicalCode, isActive, innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess)
                await _cache.RemoveAsync(CACHE_KEY, ct);

            return isSuccess;
        }

        private async Task EnsureSeededAsync(CancellationToken ct, bool commit = true)
        {
            var definitions = _slaProcessRegistry.GetProcesses();
            var aliases = _slaProcessRegistry.GetAliases();

            await _configRepo.EnsureSLAConfigsAsync(
                definitions.Select(definition => (
                    Code: definition.Code,
                    Value: definition.DefaultValue.ToString(),
                    Unit: definition.DefaultUnit)),
                aliases.Select(alias => (
                    LegacyCode: alias.LegacyCode,
                    CanonicalCode: alias.CanonicalCode)),
                ct);

            if (commit)
                await _unitOfWork.CommitAsync(ct);
        }

        private static SlaDto BuildDto(SlaProcessDefinition definition, IEnumerable<Configuration> configs)
        {
            var key = $"SLA_{definition.Code}";
            var config = configs.FirstOrDefault(item =>
                string.Equals(item.ParamKey, key, StringComparison.OrdinalIgnoreCase));

            return new SlaDto
            {
                ModuleCode = definition.Code,
                Code = definition.Code,
                DisplayName = definition.DisplayName,
                ModuleName = definition.ModuleName,
                Description = definition.Description,
                Value = ResolveValue(config?.ParamValue, definition.DefaultValue),
                Unit = ResolveUnit(config?.Description, definition.DefaultUnit),
                IsActive = config?.IsActive ?? true
            };
        }

        private static string ResolveValue(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed.ToString()
                : fallback.ToString();
        }

        private static string ResolveUnit(string? description, string fallbackUnit)
        {
            var unit = description != null && description.StartsWith("Unit: ", StringComparison.OrdinalIgnoreCase)
                ? description["Unit: ".Length..].Trim()
                : fallbackUnit;

            return string.Equals(unit, "DAYS", StringComparison.OrdinalIgnoreCase) ? "DAYS" : "HOURS";
        }
    }
}
