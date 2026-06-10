using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SalaryVariableUseCase : ISalaryVariableUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly ISourceCatalogRepository _sourceCatalogRepo;
        private readonly IPayrollSourceRegistry _sourceRegistry;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "SalaryFormulaMetadataCache";
        private const string SOURCE_CACHE_KEY = "SourceCatalogListCache";

        public SalaryVariableUseCase(
            IConfigurationRepository configRepo,
            ISourceCatalogRepository sourceCatalogRepo,
            IPayrollSourceRegistry sourceRegistry,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService)
        {
            _configRepo = configRepo;
            _sourceCatalogRepo = sourceCatalogRepo;
            _sourceRegistry = sourceRegistry;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<IEnumerable<VariableDto>> GetAllVariablesAsync(CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync("salary_variable_default_seed", async (innerCt) =>
            {
                await SeedDefaultVariablesAsync(innerCt);
                return true;
            }, cancellationToken: ct);

            var configs = await _configRepo.FetchVariableMappingsAsync(ct);
            return configs.Select(c => new VariableDto
            {
                Code = c.ParamKey.Replace("SALARY_VAR_", ""),
                Source = c.ParamValue,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToList();
        }

        public async Task<bool> DefineVariableAsync(VariableDto dto, int adminId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Source))
                throw new ArgumentException("Nguồn dữ liệu (Source) không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Mã biến lương không được để trống.");

            var normalizedCode = dto.Code.Trim().ToUpperInvariant();
            var normalizedSource = dto.Source.Trim();

            return await _lockService.GetWithLockAsync($"salary_variable_{normalizedCode.ToLowerInvariant()}", async (innerCt) =>
            {
                await SyncSystemPayrollSourcesAsync(innerCt);

                var source = await _sourceCatalogRepo.GetActiveBySourcePathAsync(normalizedSource, innerCt);
                if (source == null)
                    throw new ArgumentException("Nguồn dữ liệu không thuộc danh mục source_catalogs đang hoạt động.");

                var isSuccess = false;

                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    await _configRepo.SaveMappingAsync(
                        normalizedCode,
                        normalizedSource,
                        dto.Description,
                        dto.IsActive,
                        innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                if (isSuccess)
                    await _cache.RemoveAsync(CACHE_KEY, innerCt);

                return isSuccess;
            }, cancellationToken: ct);
        }

        public async Task<bool> SetVariableActiveAsync(string code, bool isActive, int adminId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Mã biến lương không được để trống.");

            var normalizedCode = code.Trim().ToUpperInvariant();

            return await _lockService.GetWithLockAsync($"salary_variable_{normalizedCode.ToLowerInvariant()}", async (innerCt) =>
            {
                await _configRepo.SetMappingActiveAsync(normalizedCode, isActive, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                await _cache.RemoveAsync(CACHE_KEY, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task<IReadOnlyCollection<PayrollSourceDefinition>> SyncSystemPayrollSourcesAsync(CancellationToken ct)
        {
            var sources = await _sourceRegistry.GetSourcesAsync(ct);
            await _sourceCatalogRepo.SyncSystemPayrollSourcesAsync(sources, ct);
            await _unitOfWork.CommitAsync(ct);
            await _cache.RemoveAsync(SOURCE_CACHE_KEY, ct);
            return sources;
        }

        private async Task SeedDefaultVariablesAsync(CancellationToken ct)
        {
            var sources = await SyncSystemPayrollSourcesAsync(ct);
            var variables = sources.Select(source => (
                Code: ToVariableCode(source.Code),
                Source: source.Code,
                Description: source.DisplayName));

            await _configRepo.EnsureVariableMappingsAsync(variables, ct);
            await _unitOfWork.CommitAsync(ct);
            await _cache.RemoveAsync(CACHE_KEY, ct);
        }

        private static string ToVariableCode(string sourceCode)
        {
            var chars = sourceCode
                .Trim()
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_')
                .ToArray();

            return new string(chars);
        }
    }
}
