using HRM.backend.src.HRM.Application.DTOs.TasksTraining;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases
{
    public interface IKpiManagementUseCase
    {
        Task<KpiImportResultDto> ImportKpisFromExcelAsync(KpiImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
    }
}
