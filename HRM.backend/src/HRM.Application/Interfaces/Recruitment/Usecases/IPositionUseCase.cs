using HRM.backend.src.HRM.Application.DTOs.Recruitment;

namespace HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases
{
    public interface IPositionUseCase
    {
        Task<List<PositionDto>> GetActivePositionsAsync(CancellationToken ct = default);
    }
}

