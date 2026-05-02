using global::HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    namespace HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System
    {
        public interface IAuditLogRepository : IBaseRepository<AuditLog>
        {
            Task LogAuditActionAsync(string actionType, int? adminId, string? tableName = null, string? oldVal = null, string? newVal = null);
            Task<IEnumerable<AuditLog>> FetchLogsWithDetailAsync(string? actionFilter);
        }
    }
}
