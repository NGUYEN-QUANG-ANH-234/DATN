using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IPenaltyRuleRepository : IBaseRepository<PenaltyRule>
    {
        Task<List<PenaltyRule>> GetActiveBySourceAsync(PenaltySourceType sourceType, CancellationToken ct = default);
    }
}
