using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface ISeniorAppointmentUseCase
    {
        Task<PersonnelChangeDetailDto> CreateSeniorAppointmentAsync(CreateSeniorAppointmentDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> SubmitAppointmentConsentAsync(int id, int actorAccountId, AppointmentConsentDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> StartHrContractFlowAsync(int id, int actorAccountId, HrContractFlowDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> IssueAppointmentDecisionAsync(int id, int actorAccountId, IssueAppointmentDecisionDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ExecuteSeniorAppointmentAsync(int id, int actorAccountId, ExecutePersonnelChangeDto dto, CancellationToken ct);
    }
}
