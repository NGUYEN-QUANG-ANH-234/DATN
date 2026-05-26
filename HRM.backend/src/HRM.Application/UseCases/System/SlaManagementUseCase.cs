using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SlaManagementUseCase : ISlaManagementUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "SLA_Config_Cache";

        public SlaManagementUseCase(
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

        public async Task<IEnumerable<SlaDto>> GetSLAConfigsAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchSLAByModuleAsync(innerCt);
                    return configs.Select(c => new SlaDto
                    {
                        ModuleCode = c.ParamKey.Replace("SLA_", ""),
                        Value = c.ParamValue,
                        Unit = c.Description?.Replace("Unit: ", "") ?? "HOURS"
                    }).ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> UpdateSLAParameterAsync(SlaDto dto, int adminId, CancellationToken ct = default)
        {
            if (!int.TryParse(dto.Value, out int timeValue) || timeValue <= 0)
                throw new ArgumentException("Thời gian SLA phải là một số nguyên dương (> 0).");

            if (dto.Unit.ToUpper() != "HOURS" && dto.Unit.ToUpper() != "DAYS")
                throw new ArgumentException("Đơn vị thời gian không hợp lệ. Chỉ chấp nhận 'HOURS' hoặc 'DAYS'.");

            bool isSuccess = false;

            await _lockService.GetWithLockAsync($"sla_config_{dto.ModuleCode}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    await _configRepo.UpdateSLAConfigAsync(dto.ModuleCode, dto.Value, dto.Unit.ToUpper(), innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, ct);
            }

            return isSuccess;
        }
    }
}
