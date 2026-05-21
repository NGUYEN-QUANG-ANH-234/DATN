using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.Recruitment
{
    [ApiController]
    [Route("api/v1/candidates")]
    public class CandidateController : ControllerBase
    {
        private readonly ICandidateUseCase _useCase;
        private readonly string[] _permittedExtensions = { ".pdf" };
        private const long _fileSizeLimit = 5 * 1024 * 1024; // 5MB

        public CandidateController(ICandidateUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost("apply")]
        [Consumes("multipart/form-data")]
        [AllowAnonymous] // Public cho ứng viên bên ngoài nộp
        public async Task<IActionResult> Apply([FromForm] ApplyJobDto dto, CancellationToken ct)
        {
            // 1. Fail-fast: Validate File (Giống hệt cách làm ở ProfileController)
            if (dto.CvFile == null || dto.CvFile.Length == 0)
                return BadRequest(new { Success = false, Message = "Vui lòng đính kèm CV." });

            if (dto.CvFile.Length > _fileSizeLimit)
                return BadRequest(new { Success = false, Message = "Dung lượng CV không được vượt quá 5MB." });

            var ext = Path.GetExtension(dto.CvFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
                return BadRequest(new { Success = false, Message = "Chỉ chấp nhận file định dạng .pdf" });

            if (dto.CvFile.ContentType != "application/pdf")
                return BadRequest(new { Success = false, Message = "MimeType không hợp lệ. Vui lòng tải lên đúng file PDF." });

            // 2. Chuyển UseCase xử lý nghiệp vụ
            try
            {
                var result = await _useCase.ApplyForJobAsync(dto, ct);
                return Ok(new { Success = true, Message = "Nộp hồ sơ thành công! Bộ phận nhân sự sẽ liên hệ với bạn sớm nhất.", Data = result });
            }
            catch (InvalidOperationException ex) // Bắt lỗi Business Rules
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("my-applications")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyApplications([FromQuery] string email, [FromQuery] string trackingCode, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetMyApplicationsAsync(email, trackingCode, ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        [RequirePermission("CANDIDATE_VIEW", GroupName = SystemModules.Recruitment, Description = "Xem danh sách ứng viên")]
        public async Task<IActionResult> GetAllCandidates(CancellationToken ct)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                var result = await _useCase.GetAllCandidatesAsync(userId, actorRoleName, ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("{id}/hr-approve")]
        [RequirePermission("CANDIDATE_HR_APPROVE", GroupName = SystemModules.Recruitment, Description = "Duyệt hồ sơ ứng viên vòng HR")]
        public async Task<IActionResult> HrApprove(int id, CancellationToken ct)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                await _useCase.HrApproveAsync(id, userId, actorRoleName, ct);
                return Ok(new { Success = true, Message = "Đã duyệt và chuyển hồ sơ sang vòng phỏng vấn." });
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
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("{id}/dept-confirm")]
        [RequirePermission("CANDIDATE_DEPT_APPROVE", GroupName = SystemModules.Recruitment, Description = "Xác nhận ứng viên vòng Chuyên môn")]
        public async Task<IActionResult> DeptConfirm(int id, CancellationToken ct)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                await _useCase.ConfirmByDepartmentAsync(id, userId, actorRoleName, ct);
                return Ok(new { Success = true, Message = "Xác nhận ứng viên đạt yêu cầu. Hồ sơ đã chuyển lên Giám đốc (SLA 15 ngày)." });
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
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("{id}/final-approve")]
        [RequirePermission("CANDIDATE_FINAL_APPROVE", GroupName = SystemModules.Recruitment, Description = "Chốt duyệt tuyển dụng vòng Giám đốc")]
        public async Task<IActionResult> FinalApprove(int id, CancellationToken ct)
        {
            try
            {
                // THÊM 2 DÒNG NÀY
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                await _useCase.FinalApproveAsync(id, userId, actorRoleName, ct); // TRUYỀN THÊM THAM SỐ
                return Ok(new { Success = true, Message = "Giám đốc đã chốt tuyển dụng thành công." });
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
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("{id}/reject")]
        [RequirePermission("CANDIDATE_REJECT", GroupName = SystemModules.Recruitment, Description = "Từ chối hồ sơ ứng viên")]
        public async Task<IActionResult> RejectCandidate(int id, CancellationToken ct)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                await _useCase.RejectAsync(id, userId, actorRoleName, ct);
                return Ok(new { Success = true, Message = "Đã từ chối hồ sơ ứng viên thành công." });
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
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
