using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(MyDbContext context) : base(context) { }

        public async Task LogAuditActionAsync(string actionType, int? adminId, string? tableName = null, string? oldVal = null, string? newVal = null)
        {
            var log = new AuditLog
            {
                ActionType = actionType,
                AccountId = adminId,
                TableName = tableName,
                OldValue = oldVal,
                NewValue = newVal,
                Timestamp = DateTime.UtcNow
            };
            await _dbSet.AddAsync(log);
        }

        public async Task<IEnumerable<AuditLog>> FetchLogsWithDetailAsync(string? actionFilter)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(actionFilter))
                query = query.Where(l => l.ActionType != null && l.ActionType.Contains(actionFilter));

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }
    }
}
