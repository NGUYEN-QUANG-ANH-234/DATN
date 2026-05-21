using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/audit-logs")]
    [Authorize] // Bắt buộc đăng nhập
    [RequirePermission("AUDIT_VIEW")] // Chỉ Admin hoặc người có quyền mới xem được
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditManagementUseCase _useCase;

        public AuditLogController(IAuditManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("AUDIT_LOG_VIEW", GroupName = SystemModules.SystemManagement, Description = "Xem danh sách nhật ký hệ thống")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.SearchLogsAsync(filter, ct);
                return Ok(new { success = true, data });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi truy xuất nhật ký hệ thống." });
            }
        }

        [HttpGet("dashboard-stats")]
        [RequirePermission("AUDIT_LOG_STATS_VIEW", GroupName = SystemModules.SystemManagement, Description = "Xem biểu đồ thống kê nhật ký hoạt động")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] int days = 30, CancellationToken ct = default)
        {
            try
            {
                var stats = await _useCase.GetStatisticsAsync(days, ct);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi truy xuất dữ liệu thống kê." });
            }
        }
    }
}
