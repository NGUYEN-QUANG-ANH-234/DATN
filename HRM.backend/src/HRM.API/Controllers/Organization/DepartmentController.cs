using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.Organization
{
    [ApiController]
    [Route("api/v1/departments")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IOrgTreeUseCase _orgTreeUseCase;

        public DepartmentController(IOrgTreeUseCase orgTreeUseCase)
        {
            _orgTreeUseCase = orgTreeUseCase;
        }

        [HttpGet("tree")]
        [RequirePermission("ORG_TREE_VIEW", GroupName = SystemModules.SystemManagement, Description = "Xem sơ đồ tổ chức phòng ban")]
        public async Task<IActionResult> GetTree(CancellationToken ct)
        {
            try
            {
                var tree = await _orgTreeUseCase.GetOrganizationTreeAsync(ct);
                return Ok(new { Success = true, Data = tree });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}/structure")]
        [RequirePermission("ORG_TREE_UPDATE", GroupName = SystemModules.SystemManagement, Description = "Cập nhật cấu trúc sơ đồ tổ chức")]
        public async Task<IActionResult> UpdateStructure(int id, [FromBody] UpdateDeptStructureDto dto, CancellationToken ct)
        {
            try
            {
                int actorId = User.GetAccountIdOrThrow();
                await _orgTreeUseCase.UpdateDepartmentNodeAsync(id, dto, actorId, ct);

                return Ok(new { Success = true, Message = "Cập nhật cấu trúc sơ đồ tổ chức thành công." });
            }
            catch (InvalidOperationException ex)
            {
                // Trả về 400 hoặc 409 tùy logic, ở đây lặp vòng nên ném 400
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("ORG_TREE_UPDATE", GroupName = SystemModules.SystemManagement, Description = "Cập nhật thông tin phòng ban")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto, CancellationToken ct)
        {
            try
            {
                int actorId = User.GetAccountIdOrThrow();
                await _orgTreeUseCase.UpdateDepartmentAsync(id, dto, actorId, ct);

                return Ok(new { Success = true, Message = "Cập nhật thông tin phòng ban thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/deactivate")]
        [RequirePermission("ORG_TREE_DELETE", GroupName = SystemModules.SystemManagement, Description = "Giải thể phòng ban")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            try
            {
                int actorId = User.GetAccountIdOrThrow();
                await _orgTreeUseCase.DeactivateDepartmentAsync(id, actorId, ct);

                return Ok(new { Success = true, Message = "Đã giải thể phòng ban thành công." });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi chuẩn theo Sequence Diagram: Nếu còn nhân sự -> throw DataConflictException -> 409 Conflict
                return Conflict(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("ORG_TREE_CREATE", GroupName = SystemModules.SystemManagement, Description = "Thêm mới phòng ban")]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto, CancellationToken ct)
        {
            try
            {
                int actorId = User.GetAccountIdOrThrow();
                await _orgTreeUseCase.CreateDepartmentAsync(dto, actorId, ct);

                return Ok(new { Success = true, Message = "Tạo phòng ban thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
