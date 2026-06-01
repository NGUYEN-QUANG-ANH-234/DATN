using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Route("api/v1/personnel-changes/promotions")]
    [Authorize]
    public class PromotionOfficialController : PersonnelChangeControllerBase
    {
        private readonly IPromotionOfficialUseCase _useCase;

        public PromotionOfficialController(IPromotionOfficialUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Create promotion or official conversion request")]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CreatePromotionAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPost("convert-official")]
        [RequirePermission("PERSONNEL_CHANGE_CREATE", GroupName = SystemModules.PersonnelChanges, Description = "Create official conversion request")]
        public async Task<IActionResult> CreateConvertOfficial([FromBody] CreateConvertOfficialDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.CreateConvertOfficialAsync(dto, ActorAccountId, ct);
                return Created($"/api/v1/personnel-changes/{data.Id}", new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/hr-review")]
        [RequirePermission("PERSONNEL_CHANGE_HR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "HR review promotion")]
        public async Task<IActionResult> HrReview(int id, [FromBody] ApprovePromotionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.HrReviewPromotionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/director-approve")]
        [RequirePermission("PERSONNEL_CHANGE_DIRECTOR_REVIEW", GroupName = SystemModules.PersonnelChanges, Description = "Director approve promotion")]
        public async Task<IActionResult> DirectorApprove(int id, [FromBody] ApprovePromotionDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.DirectorApprovePromotionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }

        [HttpPatch("{id:int}/execute")]
        [RequirePermission("PERSONNEL_CHANGE_EXECUTE", GroupName = SystemModules.PersonnelChanges, Description = "Execute promotion")]
        public async Task<IActionResult> Execute(int id, [FromBody] ExecutePersonnelChangeDto dto, CancellationToken ct)
        {
            return await ExecuteAsync(async () =>
            {
                var data = await _useCase.ExecutePromotionAsync(id, ActorAccountId, dto, ct);
                return Ok(new { Success = true, Data = data });
            });
        }
    }
}
