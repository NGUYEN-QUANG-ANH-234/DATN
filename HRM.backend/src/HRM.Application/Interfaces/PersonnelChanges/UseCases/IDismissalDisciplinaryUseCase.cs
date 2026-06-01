using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface IDismissalDisciplinaryUseCase
    {
        Task<PersonnelChangeDetailDto> CreateDismissalAsync(CreateDismissalDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> NotifyEmployeeAsync(int id, int actorAccountId, NotifyEmployeeDismissalDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> SubmitDismissalExplanationAsync(int id, int actorAccountId, DismissalEmployeeExplanationDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> DirectorApproveDismissalAsync(int id, int actorAccountId, DirectorApproveDismissalDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ExecuteDismissalAsync(int id, int actorAccountId, ExecutePersonnelChangeDto dto, CancellationToken ct);
    }
}
