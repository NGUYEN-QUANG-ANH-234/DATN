using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;

namespace HRM.backend.src.HRM.Application.UseCases.Recruitment
{
    public class PositionUseCase : IPositionUseCase
    {
        private readonly IPositionRepository _positionRepo;

        public PositionUseCase(IPositionRepository positionRepo)
        {
            _positionRepo = positionRepo;
        }

        public async Task<List<PositionDto>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            var positions = await _positionRepo.GetActivePositionsAsync(ct);
            return positions.Select(p => new PositionDto
            {
                Id = p.Id,
                Title = p.Title,
                JobLevel = p.JobLevel
            }).ToList();
        }
    }
}
