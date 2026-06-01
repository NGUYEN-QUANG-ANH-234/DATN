using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface IInternalTransferUseCase
    {
        Task<PersonnelChangeDetailDto> CreateInternalTransferDemandAsync(InternalTransferDemandDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> HrSelectEmployeeAsync(int id, int actorAccountId, HrSelectEmployeeDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> SubmitCurrentManagerOpinionAsync(int id, int actorAccountId, CurrentManagerOpinionDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> SubmitEmployeeConsentAsync(int id, int actorAccountId, EmployeeConsentDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> DirectorApproveTransferAsync(int id, int actorAccountId, DirectorApproveTransferDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> IssueTransferDecisionAsync(int id, int actorAccountId, IssueTransferDecisionDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ExecuteInternalTransferAsync(int id, int actorAccountId, ExecutePersonnelChangeDto dto, CancellationToken ct);
    }
}
