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
    public class SourceCatalogUseCase : ISourceCatalogUseCase
    {
        private readonly ISourceCatalogRepository _repository;
        private readonly IPayrollSourceRegistry _sourceRegistry;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public SourceCatalogUseCase(
            ISourceCatalogRepository repository,
            IPayrollSourceRegistry sourceRegistry,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _repository = repository;
            _sourceRegistry = sourceRegistry;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<IEnumerable<SourceCatalogDto>> GetAllSourceCatalogsAsync(CancellationToken ct = default)
        {
            var sources = await _lockService.GetWithLockAsync("source_catalog_registry_sync", async (innerCt) =>
            {
                return await SyncSystemPayrollSourcesAsync(innerCt);
            }, cancellationToken: ct);

            var sourcePaths = sources.Select(source => source.Code);
            var catalogs = await _repository.GetOrderedCatalogsAsync(sourcePaths, ct);
            return catalogs.Select(ToDto).ToList();
        }

        public async Task<SourceCatalogDto> SetSourceCatalogActiveAsync(int id, bool isActive, int actorId, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"source_catalog_status_{id}", async (innerCt) =>
            {
                var sources = await SyncSystemPayrollSourcesAsync(innerCt);
                var allowedPaths = sources
                    .Select(source => source.Code)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var catalog = await _repository.GetByIdAsync(id, innerCt);
                if (catalog == null || catalog.IsDeleted || !allowedPaths.Contains(catalog.SourcePath))
                    throw new ArgumentException("Nguồn hệ thống không tồn tại hoặc không thuộc registry hiện tại.");

                catalog.IsActive = isActive;
                _repository.Update(catalog);
                await _unitOfWork.CommitAsync(innerCt);

                return ToDto(catalog);
            }, cancellationToken: ct);
        }

        public async Task<bool> DeleteSourceCatalogAsync(int id, int actorId, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"source_catalog_delete_{id}", async (innerCt) =>
            {
                var catalog = await _repository.GetByIdAsync(id, innerCt);
                if (catalog == null || catalog.IsDeleted)
                    throw new ArgumentException("Nguồn dữ liệu không tồn tại.");

                if (catalog.IsActive)
                    throw new InvalidOperationException("Vui lòng tạm tắt nguồn dữ liệu trước khi xóa hẳn.");

                catalog.IsDeleted = true;
                catalog.IsActive = false;
                _repository.Update(catalog);
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task<IReadOnlyCollection<PayrollSourceDefinition>> SyncSystemPayrollSourcesAsync(CancellationToken ct)
        {
            var sources = await _sourceRegistry.GetSourcesAsync(ct);
            await _repository.SyncSystemPayrollSourcesAsync(sources, ct);
            await _unitOfWork.CommitAsync(ct);
            return sources;
        }

        private static SourceCatalogDto ToDto(SourceCatalog source)
        {
            return new SourceCatalogDto
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                SourcePath = source.SourcePath,
                Module = source.Module,
                DataType = source.DataType.ToString(),
                AggregationType = source.AggregationType.ToString(),
                IsPeriodBased = source.IsPeriodBased,
                IsActive = source.IsActive
            };
        }
    }
}
