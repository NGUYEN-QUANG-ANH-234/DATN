using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes/dismissals")]
    [Authorize]
    public class DismissalDisciplinaryController : PersonnelChangeControllerBase
    {
        private readonly IDismissalDisciplinaryUseCase _useCase;

        public DismissalDisciplinaryController(IDismissalDisciplinaryUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Create dismissal or disciplinary request")]
        public async Task<IActionResult> Create([FromBody] CreateDismissalDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CreateDismissalAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/notify-employee")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Notify employee about dismissal")]
        public async Task<IActionResult> NotifyEmployee(int id, [FromBody] NotifyEmployeeDismissalDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.NotifyEmployeeAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/employee-explanation")]
        [RequirePermission("PERSONNEL_CHANGE_EMPLOYEE_CONSENT", GroupName = SystemModules.PersonnelChanges, Description = "Employee explanation for dismissal")]
        public async Task<IActionResult> EmployeeExplanation(int id, [FromBody] DismissalEmployeeExplanationDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.SubmitDismissalExplanationAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/director-approve-dismissal")]
        [RequirePermission("PERSONNEL_CHANGE_DIRECTOR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Director approve dismissal")]
        public async Task<IActionResult> DirectorApproveDismissal(int id, [FromBody] DirectorApproveDismissalDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.DirectorApproveDismissalAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/execute")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Execute dismissal")]
        public async Task<IActionResult> Execute(int id, [FromBody] ExecutePersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ExecuteDismissalAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
