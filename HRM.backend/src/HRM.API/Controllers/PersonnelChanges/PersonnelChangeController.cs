using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes")]
    [Authorize]
    public class PersonnelChangeController : PersonnelChangeControllerBase
    {
        private readonly IPersonnelChangeUseCase _useCase;

        public PersonnelChangeController(IPersonnelChangeUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PERSONNEL_CHANGE_VIEW", GroupName = SystemModules.PersonnelChanges, Description = "View personnel change requests")]
        public async Task<IActionResult> GetList(
            [FromQuery] PersonnelChangeType? changeType,
            [FromQuery] PersonnelChangeStatus? status,
            [FromQuery] int? employeeId,
            [FromQuery] DateTime? requestedFrom,
            [FromQuery] DateTime? requestedTo,
            CancellationToken ct)
        {
            var data = await _useCase.GetListAsync(changeType, status, employeeId, requestedFrom, requestedTo, ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("{id:int}")]
        [RequirePermission("PERSONNEL_CHANGE_VIEW", GroupName = SystemModules.PersonnelChanges, Description = "View personnel change request detail")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.GetDetailAsync(id, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpGet("{id:int}/risk-summary")]
        [RequirePermission("PERSONNEL_CHANGE_VIEW", GroupName = SystemModules.PersonnelChanges, Description = "View personnel change risk summary")]
        public async Task<IActionResult> GetRiskSummary(int id, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.GetRiskSummaryAsync(id, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpGet("{id:int}/timeline")]
        [RequirePermission("PERSONNEL_CHANGE_VIEW", GroupName = SystemModules.PersonnelChanges, Description = "View personnel change timeline")]
        public async Task<IActionResult> GetTimeline(int id, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.GetTimelineAsync(id, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/cancel")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Cancel personnel change request")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelPersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CancelAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
