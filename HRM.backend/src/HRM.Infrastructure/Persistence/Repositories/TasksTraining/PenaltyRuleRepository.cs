using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining
{
    public class PenaltyRuleRepository : BaseRepository<PenaltyRule>, IPenaltyRuleRepository
    {
        public PenaltyRuleRepository(MyDbContext context) : base(context) { }

        public async Task<List<PenaltyRule>> GetActiveBySourceAsync(PenaltySourceType sourceType, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.SourceType == sourceType && r.IsActive)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
