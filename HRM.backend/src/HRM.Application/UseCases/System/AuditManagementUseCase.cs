using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System
{
    public class AuditManagementUseCase : IAuditManagementUseCase
    {
        private readonly IAuditLogRepository _auditLogRepo;

        public AuditManagementUseCase(IAuditLogRepository auditLogRepo)
        {
            _auditLogRepo = auditLogRepo;
        }

        public async Task<IEnumerable<AuditLogResponseDto>> SearchLogsAsync(AuditLogFilterDto filter, CancellationToken ct = default)
        {
            // Tối ưu SRP: UseCase chỉ điều phối, Repo làm nhiệm vụ lọc DB
            var logs = await _auditLogRepo.FetchLogsWithDetailAsync(
                filter.AccountId,
                filter.Module,
                filter.StartDate,
                filter.EndDate,
                ct);

            // Chuyển đổi dữ liệu cho Frontend
            return logs.Select(log => new AuditLogResponseDto
            {
                Id = log.Id,
                AccountId = log.AccountId,
                ActionType = log.ActionType!,
                TableName = log.TableName!,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Timestamp = log.Timestamp
            });
        }

        // Điều phối lấy dữ liệu thống kê
        public async Task<AuditStatisticsResponseDto> GetStatisticsAsync(int days, CancellationToken ct = default)
        {
            return await _auditLogRepo.GetAuditStatisticsAsync(days, ct);
        }
    }
}
