using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IAuditManagementUseCase
    {
        Task<IEnumerable<AuditLogResponseDto>> SearchLogsAsync(AuditLogFilterDto filter, CancellationToken ct = default);
        Task<AuditStatisticsResponseDto> GetStatisticsAsync(int days, CancellationToken ct = default);
    }
}
