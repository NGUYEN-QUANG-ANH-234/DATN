using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class SourceCatalogUseCase : ISourceCatalogUseCase
    {
        private readonly ISourceCatalogRepository _repository;
        private readonly IAppCache _cache;

        private const string CACHE_KEY = "SourceCatalogListCache";

        public SourceCatalogUseCase(ISourceCatalogRepository repository, IAppCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<IEnumerable<SourceCatalogDto>> GetAllSourceCatalogsAsync(CancellationToken ct = default)
        {
            // 1. Thử lấy từ Cache
            var cachedCatalogs = await _cache.GetAsync<IEnumerable<SourceCatalogDto>>(CACHE_KEY);
            if (cachedCatalogs != null)
            {
                return cachedCatalogs;
            }

            // 2. Nếu Cache trống, truy vấn DB
            var catalogs = await _repository.GetOrderedCatalogsAsync(ct);

            var catalogDtos = catalogs.Select(x => new SourceCatalogDto
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                SourcePath = x.SourcePath,
                Module = x.Module
            }).ToList(); // Dùng ToList() để đánh giá câu lệnh LINQ ngay lập tức, phục vụ việc Serialize

            // 3. Lưu vào Cache với thời gian sống là 24 giờ
            await _cache.SetAsync(
                key: CACHE_KEY,
                data: catalogDtos,
                absoluteExpireTime: TimeSpan.FromHours(24),
                unusedExpireTime: null,
                ct: ct
            );

            return catalogDtos;
        }
    }
}

