using HRM.backend.src.HRM.Application.DTOs.TasksTraining;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases
{
    public interface ITaskManagementUseCase
    {
        Task<List<TaskResponseDto>> GetMyTasksAsync(int actorAccountId, CancellationToken ct = default);
        Task<List<TaskResponseDto>> GetPendingReviewAsync(int actorAccountId, string role, CancellationToken ct = default);
        Task<TaskResponseDto> GetReviewContextAsync(int id, int actorAccountId, string role, CancellationToken ct = default);
        Task UpdateProgressAsync(int id, TaskProgressUpdateDto dto, int actorAccountId, CancellationToken ct = default);
        Task ProvideFeedbackAsync(int id, TaskFeedbackDto dto, int actorAccountId, string role, CancellationToken ct = default);
        Task ApproveTaskAsync(int id, int actorAccountId, string role, CancellationToken ct = default);
    }
}
