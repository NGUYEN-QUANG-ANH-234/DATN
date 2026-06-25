using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IHistoryTrackingUseCase
    {
        Task<PaginatedHistoryResponse> GetConsolidatedHistoryAsync(
            int accountId,
            HistoryFilterDto filter,
            CancellationToken ct = default);

        Task<PaginatedHistoryResponse> GetEmployeeConsolidatedHistoryAsync(
            int actorAccountId,
            string actorRoleName,
            int employeeId,
            HistoryFilterDto filter,
            CancellationToken ct = default);
    }
}
