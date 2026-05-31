using global::HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IAuditLogRepository : IBaseRepository<AuditLog>
    {
        Task LogSystemEventAsync(string actionType, int? accountId, string module, string? message = null);
        Task<IEnumerable<AuditLog>> FetchLogsWithDetailAsync(
        int? accountId, string? module, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
        Task<AuditStatisticsResponseDto> GetAuditStatisticsAsync(int days, CancellationToken ct = default);
    }
}
