using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class PerformanceDetailRepository : BaseRepository<PerformanceDetail>, IPerformanceDetailRepository
    {
        public PerformanceDetailRepository(MyDbContext context) : base(context) { }

        public async Task<List<PerformanceDetail>> GetByReviewAsync(int reviewId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(d => d.ReviewId == reviewId)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
