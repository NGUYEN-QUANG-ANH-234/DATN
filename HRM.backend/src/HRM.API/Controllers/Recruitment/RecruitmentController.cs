using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.Recruitment
{
    [ApiController]
    [Route("api/v1/recruitment")]
    public class RecruitmentController : ControllerBase
    {
        private readonly IRecruitmentUseCase _useCase;
        private readonly IRecruitmentRequestRepository _reqRepo;

        public RecruitmentController(IRecruitmentUseCase useCase, IRecruitmentRequestRepository reqRepo)
        {
            _useCase = useCase;
            _reqRepo = reqRepo;
        }

        [HttpPost("requests")]
        [Authorize]
        [RequirePermission("RECRUITMENT_REQUEST_CREATE", GroupName = SystemModules.Recruitment, Description = "Tạo yêu cầu tuyển dụng")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRecruitmentDto dto, CancellationToken ct)
        {
            try
            {
                // Lấy ID người đang tạo đơn
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = GetRole();

                // Truyền thêm userId xuống UseCase
                int id = await _useCase.CreateRequestAsync(dto, userId, actorRoleName, ct, GetIdempotencyKey());

                return Created($"/api/v1/recruitment/requests/{id}", new { Success = true, Data = id });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        private string? GetIdempotencyKey()
        {
            return Request.Headers["Idempotency-Key"].FirstOrDefault();
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        [HttpPatch("requests/{id}/review")]
        [Authorize]
        [RequirePermission("RECRUITMENT_REQUEST_REVIEW", GroupName = SystemModules.Recruitment, Description = "Phê duyệt yêu cầu tuyển dụng")]
        public async Task<IActionResult> ReviewRequest(int id, [FromBody] ReviewRecruitmentDto dto, CancellationToken ct)
        {
            try
            {
                int approverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // TRỌNG TÂM: Lấy RoleName của người dùng từ Token
                string actorRoleName = GetRole();

                // Truyền actorRoleName vào hàm UseCase như đã sửa ở bước trước
                await _useCase.ReviewRequestAsync(id, approverId, actorRoleName, dto, ct);

                return Ok(new { Success = true, Message = "Thao tác phê duyệt thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("active-jobs")]
        [AllowAnonymous] // Public cho Cổng Ứng viên gọi để hiển thị danh sách Job
        public async Task<IActionResult> GetActiveJobs(CancellationToken ct)
        {
            var jobs = await _reqRepo.GetActiveJobPostingsAsync(ct);
            var result = jobs.Select(j => new
            {
                j.Id,
                DepartmentName = j.Department?.DeptName,
                PositionName = j.Position?.Title,
                j.Quantity,
                j.Description,
                j.Deadline
            });
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("requests/pending")]
        [Authorize]
        [RequirePermission("RECRUITMENT_REQUEST_APPROVE_VIEW", GroupName = SystemModules.Recruitment, Description = "Xem danh sách yêu cầu tuyển dụng chờ duyệt")]
        public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
        {
            try
            {
                int actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var requests = await _useCase.GetPendingRequestsAsync(actorId, ct);

                var result = requests.Select(r => new
                {
                    r.Id,
                    r.Quantity,
                    r.Description,
                    r.Deadline,
                    Department = r.Department != null ? new { r.Department.DeptName } : null,
                    Position = r.Position != null ? new { r.Position.Title } : null
                });

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("requests/my-requests")]
        [Authorize]
        [RequirePermission("RECRUITMENT_REQUEST_SELF_VIEW", GroupName = SystemModules.Recruitment, Description = "Xem danh sách yêu cầu tuyển dụng cá nhân đã tạo")]
        public async Task<IActionResult> GetMyRequests(CancellationToken ct)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Viết một hàm trong UseCase/Repo để lấy các đơn có CreatedById == userId
            var requests = await _useCase.GetMyRequestsAsync(userId, ct);

            var result = requests.Select(r => new {
                r.Id,
                r.Quantity,
                Status = r.Status.ToString(),
                r.CreatedAt,
                PositionName = r.Position?.Title,
                DepartmentName = r.Department?.DeptName
            });

            return Ok(new { Success = true, Data = result });
        }
    }
}
