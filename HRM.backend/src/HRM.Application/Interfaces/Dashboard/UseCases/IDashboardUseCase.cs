using HRM.backend.src.HRM.Application.DTOs.Dashboard;

namespace HRM.backend.src.HRM.Application.Interfaces.Dashboard.UseCases
{
    public interface IDashboardUseCase
    {
        Task<DashboardResponseDto> GetDashboardAsync(int accountId, string role, int? month, int? year, CancellationToken ct);
        Task<DashboardDrilldownDto> GetDrilldownAsync(int accountId, string role, string type, int? month, int? year, string? scope, CancellationToken ct);
    }
}
