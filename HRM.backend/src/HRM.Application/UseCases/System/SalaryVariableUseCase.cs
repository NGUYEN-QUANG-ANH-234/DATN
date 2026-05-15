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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;

        private const string CACHE_KEY = "SalaryFormulaMetadataCache";

        public SalaryVariableUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<IEnumerable<VariableDto>> GetAllVariablesAsync(CancellationToken ct = default)
        {
            var cachedVariables = await _cache.GetAsync<IEnumerable<VariableDto>>(CACHE_KEY);
            if (cachedVariables != null)
            {
                return cachedVariables;
            }

            var configs = await _configRepo.FetchVariableMappingsAsync(ct);
            var variables = configs.Select(c => new VariableDto
            {
                Code = c.ParamKey.Replace("SALARY_VAR_", ""),
                Source = c.ParamValue,
                Description = c.Description
            }).ToList();

            await _cache.SetAsync(CACHE_KEY, variables, TimeSpan.FromHours(24), null, ct);

            return variables;
        }

        public async Task<bool> DefineVariableAsync(VariableDto dto, int adminId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Source))
                throw new ArgumentException("Nguồn dữ liệu (Source) không được để trống.");

            bool isSuccess = false;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _configRepo.SaveMappingAsync(dto.Code, dto.Source, ct);

                // Ghi log đã được chuyển giao cho DbContext Hook lo liệu

                await _unitOfWork.CommitAsync(ct);
                isSuccess = true;
            }, ct);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, ct);
            }

            return isSuccess;
        }
    }
}