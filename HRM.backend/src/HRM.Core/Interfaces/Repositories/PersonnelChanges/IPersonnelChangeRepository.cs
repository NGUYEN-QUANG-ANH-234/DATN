using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges
{
    public interface IPersonnelChangeRepository : IBaseRepository<PersonnelChangeRequest>
    {
        new Task<PersonnelChangeRequest?> GetByIdAsync(int id, CancellationToken ct = default);
        new Task AddAsync(PersonnelChangeRequest request, CancellationToken ct = default);
        new void Update(PersonnelChangeRequest request);
        Task<PersonnelChangeRequest?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<List<PersonnelChangeRequest>> GetByFilterAsync(
            PersonnelChangeType? changeType,
            PersonnelChangeStatus? status,
            int? employeeId,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            CancellationToken ct = default);

        Task<List<PersonnelChangeRequest>> GetByContractFlowReferenceAsync(
            int? contractId,
            int? contractAddendumId,
            CancellationToken ct = default);

        Task<List<PersonnelChangeHistory>> GetTimelineAsync(int requestId, CancellationToken ct = default);
        Task AddHistoryAsync(PersonnelChangeHistory history, CancellationToken ct = default);
        Task AddApprovalAsync(PersonnelChangeApproval approval, CancellationToken ct = default);
        Task AddContractLinkAsync(PersonnelChangeContractLink link, CancellationToken ct = default);
        Task AddRiskSnapshotAsync(PersonnelChangeRiskSnapshot snapshot, CancellationToken ct = default);
    }
}
