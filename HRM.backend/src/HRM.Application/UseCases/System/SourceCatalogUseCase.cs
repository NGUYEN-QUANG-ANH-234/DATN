using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SourceCatalogUseCase : ISourceCatalogUseCase
    {
        private readonly ISourceCatalogRepository _repository;
        private readonly IAppCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "SourceCatalogListCache";

        public SourceCatalogUseCase(ISourceCatalogRepository repository, IAppCache cache, IUnitOfWork unitOfWork, ILockService lockService)
        {
            _repository = repository;
            _cache = cache;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<IEnumerable<SourceCatalogDto>> GetAllSourceCatalogsAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    await _repository.EnsureDefaultPayrollCatalogsAsync(innerCt);
                    await _unitOfWork.CommitAsync(innerCt);

                    var catalogs = await _repository.GetOrderedCatalogsAsync(innerCt);
                    return catalogs.Select(x => new SourceCatalogDto
                    {
                        Id = x.Id,
                        DisplayName = x.DisplayName,
                        SourcePath = x.SourcePath,
                        Module = x.Module,
                        DataType = x.DataType.ToString(),
                        AggregationType = x.AggregationType.ToString(),
                        IsPeriodBased = x.IsPeriodBased,
                        IsActive = x.IsActive
                    }).ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<SourceCatalogDto> CreateSourceCatalogAsync(CreateSourceCatalogDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.DisplayName))
                throw new ArgumentException("Tên nguồn dữ liệu không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.SourcePath))
                throw new ArgumentException("SourcePath không được để trống.");

            var normalizedSourcePath = dto.SourcePath.Trim();

            return await _lockService.GetWithLockAsync($"source_catalog_{normalizedSourcePath.ToLowerInvariant()}", async (innerCt) =>
            {
            if (await _repository.GetActiveBySourcePathAsync(normalizedSourcePath, innerCt) != null)
                throw new ArgumentException("SourcePath đã tồn tại trong danh mục đang hoạt động.");
            if (!Enum.TryParse<SalaryVariableDataType>(dto.DataType, true, out var dataType))
                throw new ArgumentException("DataType không hợp lệ.");
            if (!Enum.TryParse<SalaryAggregationType>(dto.AggregationType, true, out var aggregationType))
                throw new ArgumentException("AggregationType không hợp lệ.");

            var entity = new SourceCatalog
            {
                DisplayName = dto.DisplayName.Trim(),
                SourcePath = normalizedSourcePath,
                Module = dto.Module.Trim(),
                DataType = dataType,
                AggregationType = aggregationType,
                IsPeriodBased = dto.IsPeriodBased,
                IsActive = dto.IsActive
            };

            await _repository.AddAsync(entity, innerCt);
            await _unitOfWork.CommitAsync(innerCt);
            await _cache.RemoveAsync(CACHE_KEY, innerCt);

            return new SourceCatalogDto
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName,
                SourcePath = entity.SourcePath,
                Module = entity.Module,
                DataType = entity.DataType.ToString(),
                AggregationType = entity.AggregationType.ToString(),
                IsPeriodBased = entity.IsPeriodBased,
                IsActive = entity.IsActive
            };
            }, cancellationToken: ct);
        }
    }
}

