using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/payroll-policies")]
    [Authorize(Roles = "Admin,HR")]
    public class PayrollPolicyController : ControllerBase
    {
        private readonly IPayrollPolicyUseCase _useCase;

        public PayrollPolicyController(IPayrollPolicyUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PAYROLL_POLICY_VIEW", GroupName = SystemModules.Config, Description = "Xem cấu hình chính sách lương")]
        public async Task<IActionResult> GetPolicies([FromQuery] PayrollPolicyType? policyType, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        {
            var data = await _useCase.GetPoliciesAsync(new PayrollPolicyFilterDto
            {
                PolicyType = policyType,
                IncludeInactive = includeInactive
            }, ct);

            return Ok(new { success = true, data });
        }

        [HttpPost]
        [RequirePermission("PAYROLL_POLICY_CREATE", GroupName = SystemModules.Config, Description = "Thêm chính sách lương")]
        public async Task<IActionResult> Create([FromBody] CreatePayrollPolicyDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CreatePolicyAsync(dto, User.GetAccountIdOrThrow(), ct);
                return StatusCode(201, new { success = true, message = "Da them chinh sach luong.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Cập nhật chính sách lương")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePayrollPolicyDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.UpdatePolicyAsync(id, dto, User.GetAccountIdOrThrow(), ct);
                return Ok(new { success = true, message = "Da tao phiên bản moi cua chinh sach luong.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/status")]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Bật/tắt chính sách lương")]
        public async Task<IActionResult> SetStatus(int id, [FromQuery] bool isActive, CancellationToken ct)
        {
            try
            {
                await _useCase.SetActiveAsync(id, isActive, User.GetAccountIdOrThrow(), ct);
                return Ok(new { success = true, message = "Đã cập nhật trạng thái chính sách lương." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Xóa hẳn chính sách lương")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _useCase.DeletePolicyAsync(id, User.GetAccountIdOrThrow(), ct);
                return Ok(new { success = true, message = "Đã xóa chính sách lương." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("overtime-rates")]
        [RequirePermission("PAYROLL_POLICY_VIEW", GroupName = SystemModules.Config, Description = "Xem hệ số làm thêm")]
        public async Task<IActionResult> GetOvertimeRates([FromQuery] bool includeInactive = true, CancellationToken ct = default)
        {
            var data = await _useCase.GetOvertimeRateConfigsAsync(includeInactive, ct);
            return Ok(new { success = true, data });
        }

        [HttpPost("overtime-rates")]
        [RequirePermission("PAYROLL_POLICY_CREATE", GroupName = SystemModules.Config, Description = "Thêm hệ số làm thêm")]
        public async Task<IActionResult> CreateOvertimeRate([FromBody] SaveOvertimeRateConfigDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CreateOvertimeRateConfigAsync(dto, User.GetAccountIdOrThrow(), ct);
                return StatusCode(201, new { success = true, message = "Đã thêm hệ số làm thêm.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("overtime-rates/{id:int}")]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Tạo phiên bản hệ số làm thêm")]
        public async Task<IActionResult> CreateOvertimeRateVersion(int id, [FromBody] SaveOvertimeRateConfigDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CreateOvertimeRateConfigVersionAsync(id, dto, User.GetAccountIdOrThrow(), ct);
                return Ok(new { success = true, message = "Đã tạo phiên bản hệ số làm thêm.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("overtime-rates/{id:int}/status")]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Bật/tắt hệ số làm thêm")]
        public async Task<IActionResult> SetOvertimeRateStatus(int id, [FromQuery] bool isActive, CancellationToken ct)
        {
            try
            {
                await _useCase.SetOvertimeRateConfigActiveAsync(id, isActive, User.GetAccountIdOrThrow(), ct);
                return Ok(new { success = true, message = "Đã cập nhật trạng thái hệ số làm thêm." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }
    }
}
