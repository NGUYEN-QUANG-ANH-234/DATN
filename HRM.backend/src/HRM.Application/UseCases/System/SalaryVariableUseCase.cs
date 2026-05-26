using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SalaryVariableUseCase : ISalaryVariableUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly ISourceCatalogRepository _sourceCatalogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "SalaryFormulaMetadataCache";

        public SalaryVariableUseCase(
            IConfigurationRepository configRepo,
            ISourceCatalogRepository sourceCatalogRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService)
        {
            _configRepo = configRepo;
            _sourceCatalogRepo = sourceCatalogRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<IEnumerable<VariableDto>> GetAllVariablesAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchVariableMappingsAsync(innerCt);
                    return configs.Select(c => new VariableDto
                    {
                        Code = c.ParamKey.Replace("SALARY_VAR_", ""),
                        Source = c.ParamValue,
                        Description = c.Description
                    }).ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> DefineVariableAsync(VariableDto dto, int adminId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Source))
                throw new ArgumentException("Nguồn dữ liệu (Source) không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Mã biến lương không được để trống.");

            return await _lockService.GetWithLockAsync($"salary_variable_{dto.Code.Trim().ToLowerInvariant()}", async (innerCt) =>
            {
            await _sourceCatalogRepo.EnsureDefaultPayrollCatalogsAsync(innerCt);
            await _unitOfWork.CommitAsync(innerCt);
            var source = await _sourceCatalogRepo.GetActiveBySourcePathAsync(dto.Source, innerCt);
            if (source == null)
                throw new ArgumentException("Nguồn dữ liệu không thuộc danh mục source_catalogs đang hoạt động.");

            bool isSuccess = false;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _configRepo.SaveMappingAsync(dto.Code, dto.Source, dto.Description, innerCt);

                await _unitOfWork.CommitAsync(innerCt);
                isSuccess = true;
            }, innerCt);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, innerCt);
            }

            return isSuccess;
            }, cancellationToken: ct);
        }
    }
}
