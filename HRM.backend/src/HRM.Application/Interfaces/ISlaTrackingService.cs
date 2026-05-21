using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface ISlaTrackingService
    {
        Task CreateTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default);
        Task ResolveTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default);
    }
}
