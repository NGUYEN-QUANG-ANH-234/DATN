using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface IPersonnelChangeUseCase
    {
        Task<List<PersonnelChangeListItemDto>> GetListAsync(
            PersonnelChangeType? changeType,
            PersonnelChangeStatus? status,
            int? employeeId,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            CancellationToken ct);
        Task<List<PersonnelChangeListItemDto>> GetMyActionItemsAsync(int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> GetDetailAsync(int id, CancellationToken ct);
        Task<PersonnelChangeRiskSummaryDto> GetRiskSummaryAsync(int id, CancellationToken ct);
        Task<List<PersonnelChangeTimelineDto>> GetTimelineAsync(int id, CancellationToken ct);

        Task<PersonnelChangeDetailDto> CancelAsync(int id, int actorAccountId, CancelPersonnelChangeDto dto, CancellationToken ct);
    }
}
