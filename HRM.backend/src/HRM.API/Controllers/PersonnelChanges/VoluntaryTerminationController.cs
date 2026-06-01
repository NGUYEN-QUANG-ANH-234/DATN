using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes/voluntary-terminations")]
    [Authorize]
    public class VoluntaryTerminationController : PersonnelChangeControllerBase
    {
        private readonly IVoluntaryTerminationUseCase _useCase;

        public VoluntaryTerminationController(IVoluntaryTerminationUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Submit voluntary termination request")]
        public async Task<IActionResult> Submit([FromBody] SubmitResignationDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.SubmitResignationAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/manager-review")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Manager review voluntary termination")]
        public async Task<IActionResult> ManagerReview(int id, [FromBody] ManagerReviewResignationDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ManagerReviewResignationAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/hr-review")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "HR review voluntary termination")]
        public async Task<IActionResult> HrReview(int id, [FromBody] HrReviewResignationDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.HrReviewResignationAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/director-approve")]
        [RequirePermission("PERSONNEL_CHANGE_DIRECTOR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Director approve voluntary termination")]
        public async Task<IActionResult> DirectorApprove(int id, [FromBody] DirectorApproveResignationDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.DirectorApproveResignationAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/execute")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Execute voluntary termination")]
        public async Task<IActionResult> Execute(int id, [FromBody] ExecutePersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ExecuteResignationAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
