using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services
{
    public interface IPersonnelChangeContractFlowService
    {
        PersonnelChangeStatus ResolveAfterDirectorApproval(PersonnelChangeRequest request);
        bool IsContractFlowCompleted(PersonnelChangeRequest request);
        void EnsureCanExecute(PersonnelChangeRequest request);
        Task CreateContractFlowAsync(PersonnelChangeRequest request, CancellationToken ct);
        Task MarkContractFlowNegotiatingAsync(int contractId, string? note, CancellationToken ct);
        Task MarkContractFlowCompletedAsync(int contractFlowReferenceId, CancellationToken ct);
        Task MarkContractFlowCompletedAsync(int? contractId, int? contractAddendumId, CancellationToken ct);
        Task MarkContractFlowRejectedAsync(int? contractId, int? contractAddendumId, string? reason, CancellationToken ct);
    }
}
