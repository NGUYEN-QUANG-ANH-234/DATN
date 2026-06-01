using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes/internal-transfers")]
    [Authorize]
    public class InternalTransferController : PersonnelChangeControllerBase
    {
        private readonly IInternalTransferUseCase _useCase;

        public InternalTransferController(IInternalTransferUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost("demands")]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Create an internal transfer demand")]
        public async Task<IActionResult> CreateDemand([FromBody] InternalTransferDemandDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CreateInternalTransferDemandAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/hr-select-employee")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "HR select employee for internal transfer")]
        public async Task<IActionResult> HrSelectEmployee(int id, [FromBody] HrSelectEmployeeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.HrSelectEmployeeAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/current-manager-opinion")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Current manager opinion for internal transfer")]
        public async Task<IActionResult> CurrentManagerOpinion(int id, [FromBody] CurrentManagerOpinionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.SubmitCurrentManagerOpinionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/employee-consent")]
        [RequirePermission("PERSONNEL_CHANGE_EMPLOYEE_CONSENT", GroupName = SystemModules.PersonnelChanges, Description = "Employee consent for internal transfer")]
        public async Task<IActionResult> EmployeeConsent(int id, [FromBody] EmployeeConsentDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.SubmitEmployeeConsentAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/director-approve-transfer")]
        [RequirePermission("PERSONNEL_CHANGE_DIRECTOR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Director approve internal transfer")]
        public async Task<IActionResult> DirectorApproveTransfer(int id, [FromBody] DirectorApproveTransferDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.DirectorApproveTransferAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/issue-transfer-decision")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Issue internal transfer decision")]
        public async Task<IActionResult> IssueTransferDecision(int id, [FromBody] IssueTransferDecisionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.IssueTransferDecisionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/execute")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Execute internal transfer")]
        public async Task<IActionResult> Execute(int id, [FromBody] ExecutePersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ExecuteInternalTransferAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
