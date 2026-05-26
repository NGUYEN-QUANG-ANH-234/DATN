using HRM.backend.src.HRM.Core.Entities.TasksTraining;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IPerformanceDetailRepository : IBaseRepository<PerformanceDetail>
    {
        Task<List<PerformanceDetail>> GetByReviewAsync(int reviewId, CancellationToken ct = default);
    }
}
