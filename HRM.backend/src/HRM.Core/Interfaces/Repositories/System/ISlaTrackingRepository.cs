using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface ISlaTrackingRepository : IBaseRepository<SlaTrackingTask>
    {
        // Hàm phục vụ đóng SLA khi HR duyệt
        Task<SlaTrackingTask?> GetPendingTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default);

        // Hàm phục vụ cho CentralSlaWorker chạy ngầm
        Task<List<SlaTrackingTask>> GetViolatedTasksAsync(DateTime currentTime, CancellationToken ct = default);
    }
}
