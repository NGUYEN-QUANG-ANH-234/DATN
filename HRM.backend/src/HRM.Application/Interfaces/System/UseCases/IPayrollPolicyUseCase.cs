using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IPayrollPolicyUseCase
    {
        Task<List<PayrollPolicyDto>> GetPoliciesAsync(PayrollPolicyFilterDto filter, CancellationToken ct = default);
        Task<PayrollPolicyDto> CreatePolicyAsync(CreatePayrollPolicyDto dto, int actorId, CancellationToken ct = default);
        Task<PayrollPolicyDto> UpdatePolicyAsync(int id, UpdatePayrollPolicyDto dto, int actorId, CancellationToken ct = default);
        Task<bool> SetActiveAsync(int id, bool isActive, int actorId, CancellationToken ct = default);
        Task<bool> DeletePolicyAsync(int id, int actorId, CancellationToken ct = default);
        Task<List<OvertimeRateConfigDto>> GetOvertimeRateConfigsAsync(bool includeInactive = true, CancellationToken ct = default);
        Task<OvertimeRateConfigDto> CreateOvertimeRateConfigAsync(SaveOvertimeRateConfigDto dto, int actorId, CancellationToken ct = default);
        Task<OvertimeRateConfigDto> CreateOvertimeRateConfigVersionAsync(int id, SaveOvertimeRateConfigDto dto, int actorId, CancellationToken ct = default);
        Task<bool> SetOvertimeRateConfigActiveAsync(int id, bool isActive, int actorId, CancellationToken ct = default);
    }
}
