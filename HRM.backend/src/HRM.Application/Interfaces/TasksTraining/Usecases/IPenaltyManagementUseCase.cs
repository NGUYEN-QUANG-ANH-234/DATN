using HRM.backend.src.HRM.Application.DTOs.TasksTraining;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases
{
    public interface IPenaltyManagementUseCase
    {
        Task<List<PenaltyRecordResponseDto>> GetRecordsAsync(string? status, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<List<PenaltyRecordResponseDto>> GetMyRecordsAsync(int actorAccountId, CancellationToken ct = default);
        Task<List<PenaltyRecordResponseDto>> GetEmployeeHistoryAsync(int employeeId, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<PenaltyRecordResponseDto> GetDetailAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<PenaltyRecordResponseDto> CreateManualAsync(CreateManualPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<PenaltyRecordResponseDto> SubmitExplanationAsync(int id, SubmitPenaltyExplanationDto dto, int actorAccountId, CancellationToken ct = default);
        Task<PenaltyRecordResponseDto> ReviewByHrAsync(int id, ReviewPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<PenaltyRecordResponseDto> ReviewByDirectorAsync(int id, ReviewPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
    }
}
