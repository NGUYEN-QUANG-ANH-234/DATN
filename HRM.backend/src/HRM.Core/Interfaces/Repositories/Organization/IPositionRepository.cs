using HRM.backend.src.HRM.Core.Entities.Organization;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization
{
    public interface IPositionRepository : IBaseRepository<Position>
    {
        Task<List<Position>> GetAllActivePositionsAsync(CancellationToken ct = default);
        Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default);
    }
}
