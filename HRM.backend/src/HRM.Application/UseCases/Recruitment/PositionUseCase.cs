using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;

namespace HRM.backend.src.HRM.Application.UseCases.Recruitment
{
    public class PositionUseCase : IPositionUseCase
    {
        private const string CacheKey = "positions_active";

        private readonly IPositionRepository _positionRepo;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        public PositionUseCase(IPositionRepository positionRepo, IAppCache cache, ILockService lockService)
        {
            _positionRepo = positionRepo;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<List<PositionDto>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CacheKey,
                async (innerCt) =>
                {
                    var positions = await _positionRepo.GetActivePositionsAsync(innerCt);
                    return positions.Select(p => new PositionDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        JobLevel = p.JobLevel
                    }).ToList();
                },
                TimeSpan.FromHours(12),
                _lockService,
                ct: ct);
        }
    }
}
