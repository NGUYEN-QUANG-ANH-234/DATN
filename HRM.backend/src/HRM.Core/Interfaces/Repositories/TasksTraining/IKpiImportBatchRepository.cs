using HRM.backend.src.HRM.Core.Entities.TasksTraining;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining
{
    public interface IKpiImportBatchRepository : IBaseRepository<KpiImportBatch>
    {
        Task<List<KpiImportBatch>> GetByDeptPeriodAsync(int deptId, string period, CancellationToken ct = default);
    }
}
