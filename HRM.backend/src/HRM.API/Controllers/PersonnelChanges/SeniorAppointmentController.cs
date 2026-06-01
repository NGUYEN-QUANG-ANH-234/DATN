using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes/senior-appointments")]
    [Authorize]
    public class SeniorAppointmentController : PersonnelChangeControllerBase
    {
        private readonly ISeniorAppointmentUseCase _useCase;

        public SeniorAppointmentController(ISeniorAppointmentUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Create senior appointment request")]
        public async Task<IActionResult> Create([FromBody] CreateSeniorAppointmentDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CreateSeniorAppointmentAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/appointment-consent")]
        [RequirePermission("PERSONNEL_CHANGE_EMPLOYEE_CONSENT", GroupName = SystemModules.PersonnelChanges, Description = "Employee consent for senior appointment")]
        public async Task<IActionResult> AppointmentConsent(int id, [FromBody] AppointmentConsentDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.SubmitAppointmentConsentAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/hr-contract-flow")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Start senior appointment contract flow")]
        public async Task<IActionResult> HrContractFlow(int id, [FromBody] HrContractFlowDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.StartHrContractFlowAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/issue-appointment-decision")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Issue senior appointment decision")]
        public async Task<IActionResult> IssueAppointmentDecision(int id, [FromBody] IssueAppointmentDecisionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.IssueAppointmentDecisionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/execute")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Execute senior appointment")]
        public async Task<IActionResult> Execute(int id, [FromBody] ExecutePersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ExecuteSeniorAppointmentAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
