using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface IVoluntaryTerminationUseCase
    {
        Task<PersonnelChangeDetailDto> SubmitResignationAsync(SubmitResignationDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ManagerReviewResignationAsync(int id, int actorAccountId, ManagerReviewResignationDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> HrReviewResignationAsync(int id, int actorAccountId, HrReviewResignationDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> DirectorApproveResignationAsync(int id, int actorAccountId, DirectorApproveResignationDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ExecuteResignationAsync(int id, int actorAccountId, ExecutePersonnelChangeDto dto, CancellationToken ct);
    }
}
