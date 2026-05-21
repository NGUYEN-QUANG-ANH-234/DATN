using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.EmployeeProfile
{
    [ApiController]
    [Route("api/v1/employees")]
    [Authorize]
    public class EmployeeProfileController : ControllerBase
    {
        private readonly IManageProfileUseCase _useCase;
        private readonly IHistoryTrackingUseCase _historyUseCase;
        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long _fileSizeLimit = 5 * 1024 * 1024; // 5MB

        public EmployeeProfileController(IManageProfileUseCase useCase, IHistoryTrackingUseCase historyUseCase)
        {
            _useCase = useCase;
            _historyUseCase = historyUseCase;
        }

        [HttpPatch("profile")]
        [Consumes("multipart/form-data")]
        [RequirePermission("PROFILE_SELF_UPDATE", GroupName = SystemModules.ProfileContract, Description = "Gửi yêu cầu cập nhật hồ sơ cá nhân")]
        public async Task<IActionResult> UpdateProfile([FromForm] ProfileUpdateRequestDto dto, CancellationToken ct)
        {
            var filesToValidate = new[] { dto.IdentityFrontFile, dto.IdentityBackFile };
            foreach (var file in filesToValidate)
            {
                if (file != null)
                {
                    if (file.Length > _fileSizeLimit)
                        return BadRequest(new { Success = false, Message = "Chỉ chấp nhận file có dung lượng < 5MB." });

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
                        return BadRequest(new { Success = false, Message = "Định dạng không hợp lệ. Chỉ chấp nhận ảnh (JPG/PNG) hoặc PDF." });
                }
            }

            try
            {
                int employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _useCase.RequestProfileUpdateAsync(employeeId, dto, ct);
                return Ok(new { Success = true, Message = "Cập nhật hồ sơ thành công. Vui lòng chờ HR phê duyệt." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "CONFLICT_IDENTITY")
            {
                return Conflict(new { Success = false, Message = "Thông tin định danh (Số CCCD) đã được đăng ký trên hệ thống." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xử lý hệ thống: " + ex.Message });
            }
        }

        [HttpGet("me/profile")]
        [RequirePermission("PROFILE_SELF_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem thông tin hồ sơ cá nhân")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            int employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await _useCase.GetMyProfileAsync(employeeId, ct);

            if (profile == null)
                return NotFound(new { Success = false, Message = "Không tìm thấy hồ sơ nhân sự." });

            return Ok(new { Success = true, Data = profile });
        }

        [HttpGet("me/contracts")]
        [RequirePermission("CONTRACT_SELF_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem danh sách hợp đồng cá nhân")]
        public async Task<IActionResult> GetMyContracts(CancellationToken ct)
        {
            int employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var contracts = await _useCase.GetMyContractsAsync(employeeId, ct);
            return Ok(new { Success = true, Data = contracts });
        }

        [HttpGet("me/history")]
        [RequirePermission("PROFILE_SELF_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem lịch sử biến động hồ sơ, hợp đồng và phụ lục")]
        public async Task<IActionResult> GetMyHistory([FromQuery] HistoryFilterDto filter, CancellationToken ct)
        {
            try
            {
                int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var history = await _historyUseCase.GetConsolidatedHistoryAsync(accountId, filter, ct);
                return Ok(new { Success = true, Data = history });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xử lý hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("profile-requests/{id}/review")]
        [RequirePermission("PROFILE_REQUEST_REVIEW", GroupName = SystemModules.ProfileContract, Description = "Phê duyệt hoặc từ chối yêu cầu cập nhật hồ sơ từ nhân viên")]
        public async Task<IActionResult> ReviewProfileRequest(int id, [FromBody] ReviewProfileUpdateDto dto, CancellationToken ct)
        {
            try
            {
                int hrAccountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // TRỌNG TÂM: Lấy RoleName từ Token người dùng (ví dụ: "HR")
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                // Truyền thêm actorRoleName xuống tầng UseCase
                await _useCase.ReviewProfileUpdateAsync(id, hrAccountId, actorRoleName, dto, ct);

                return Ok(new
                {
                    Success = true,
                    Message = dto.IsApproved ? "Đã phê duyệt và cập nhật hồ sơ thành công." : "Đã từ chối yêu cầu của nhân viên."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xử lý hệ thống: " + ex.Message });
            }
        }

        [HttpGet("profile-requests/pending")]
        [RequirePermission("PROFILE_REQUEST_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem danh sách yêu cầu cập nhật hồ sơ đang chờ duyệt")]
        public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
        {
            try
            {
                var requests = await _useCase.GetPendingProfileRequestsAsync(ct);
                return Ok(new { Success = true, Data = requests });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
