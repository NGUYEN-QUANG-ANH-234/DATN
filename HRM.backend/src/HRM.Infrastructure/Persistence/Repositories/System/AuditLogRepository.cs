using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        // BẢO MẬT: Whitelist - Chỉ cho phép truy vấn các module này, chặn đứng nguy cơ lộ CSDL
        private readonly string[] ALLOWED_MODULES = { "accounts", "role_permissions", "configurations", "employees", "payrolls", "time_attendance", "personnel_change_requests" };

        public AuditLogRepository(MyDbContext context) : base(context) { }

        public async Task LogSystemEventAsync(string actionType, int? accountId, string module, string? message = null)
        {
            var safeAccountId = await NormalizeAccountIdAsync(accountId);
            var log = new AuditLog
            {
                ActionType = actionType,
                AccountId = safeAccountId,
                TableName = module,
                NewValues = message != null ? $"{{\"Message\": \"{message}\"}}" : null,
                AffectedColumns = "[]",
                Timestamp = DateTime.UtcNow
            };

            await _dbSet.AddAsync(log);
        }

        private async Task<int?> NormalizeAccountIdAsync(int? accountId)
        {
            if (!accountId.HasValue || accountId.Value <= 0)
                return null;

            var exists = await _context.Accounts.AnyAsync(a => a.Id == accountId.Value);
            return exists ? accountId.Value : null;
        }

        public async Task<IEnumerable<AuditLog>> FetchLogsWithDetailAsync(
            int? accountId, string? module, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            var query = _dbSet.Include(l => l.Account).AsQueryable();

            if (accountId.HasValue)
                query = query.Where(l => l.AccountId == accountId);

            // BẢO MẬT: So khớp Module với Whitelist
            if (!string.IsNullOrEmpty(module) && ALLOWED_MODULES.Contains(module.ToLower()))
            {
                query = query.Where(l => l.TableName == module.ToLower());
            }

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate);

            if (endDate.HasValue)
                // Cố định lấy đến giây cuối cùng của ngày đó
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(l => l.Timestamp <= endOfDay);
                }

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync(ct);
        }

        // TÍNH NĂNG MỚI: Truy xuất thống kê cho Dashboard
        public async Task<AuditStatisticsResponseDto> GetAuditStatisticsAsync(int days, CancellationToken ct = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var query = _dbSet.Where(x => x.Timestamp >= cutoff);

            var moduleStats = await query
                .GroupBy(x => x.TableName)
                .Select(g => new ModuleStatDto { Module = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var actionStats = await query
                .GroupBy(x => x.ActionType)
                .Select(g => new ActionStatDto { Action = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return new AuditStatisticsResponseDto
                {
                    ModuleStats = moduleStats,
                    ActionStats = actionStats
                };
            }
        }
    }
