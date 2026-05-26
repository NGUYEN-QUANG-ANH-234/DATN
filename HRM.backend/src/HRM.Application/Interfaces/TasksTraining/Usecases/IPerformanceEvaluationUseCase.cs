using HRM.backend.src.HRM.Application.DTOs.TasksTraining;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases
{
    public interface IPerformanceEvaluationUseCase
    {
        Task<List<PerformanceEvaluationDto>> GetMyReviewsAsync(int actorAccountId, CancellationToken ct = default);
        Task<List<PerformanceEvaluationDto>> GetPendingEvaluationsAsync(int actorAccountId, string role, CancellationToken ct = default);
        Task<PerformanceEvaluationDto> GetDetailAsync(int id, int actorAccountId, string role, CancellationToken ct = default);
        Task UpdateMyProgressAsync(int id, PerformanceProgressUpdateDto dto, int actorAccountId, CancellationToken ct = default);
        Task FinalizeScoreAsync(int id, FinalizePerformanceDto dto, int actorAccountId, string role, CancellationToken ct = default);
    }
}
