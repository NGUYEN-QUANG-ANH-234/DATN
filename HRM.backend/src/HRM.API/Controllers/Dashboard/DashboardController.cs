using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.Interfaces.Dashboard.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.Dashboard
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardUseCase _dashboardUseCase;

        public DashboardController(IDashboardUseCase dashboardUseCase)
        {
            _dashboardUseCase = dashboardUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
        {
            var data = await _dashboardUseCase.GetDashboardAsync(User.GetAccountIdOrThrow(), GetRole(), month, year, ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("drilldowns/{type}")]
        public async Task<IActionResult> GetDrilldown(string type, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? scope, CancellationToken ct)
        {
            var data = await _dashboardUseCase.GetDrilldownAsync(User.GetAccountIdOrThrow(), GetRole(), type, month, year, scope, ct);
            return Ok(new { Success = true, Data = data });
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
