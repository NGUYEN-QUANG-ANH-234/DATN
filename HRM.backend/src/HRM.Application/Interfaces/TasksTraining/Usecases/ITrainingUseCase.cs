using HRM.backend.src.HRM.Application.DTOs.TasksTraining;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases
{
    public interface ITrainingUseCase
    {
        Task<List<TrainingSummaryDto>> GetMyLearningAsync(int actorAccountId, CancellationToken ct = default);
        Task<TrainingSummaryDto> GetTrainingReportAsync(int trainingId, int actorAccountId, string role, CancellationToken ct = default);
        Task<List<TrainingSummaryDto>> GetPendingEvaluationsAsync(int actorAccountId, string role, CancellationToken ct = default);
        Task EvaluateProcessAsync(EvaluateTrainingDto dto, int actorAccountId, string role, CancellationToken ct = default);
    }
}
