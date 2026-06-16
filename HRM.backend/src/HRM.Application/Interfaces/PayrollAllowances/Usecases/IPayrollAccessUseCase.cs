using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IPayrollAccessUseCase
    {
        Task<List<SalarySlipDto>> GetSalarySlipsAsync(int accountId, string role, string? period, byte? month, short? year, CancellationToken ct = default);
        Task<SalarySlipDto> GetSalarySlipDetailAsync(int accountId, string role, int slipId, CancellationToken ct = default);
        Task<List<SalarySlipDto>> GetMySalarySlipsAsync(int accountId, string? period, byte? month, short? year, CancellationToken ct = default);
        Task<SalarySlipDto> GetMySalarySlipDetailAsync(int accountId, int slipId, CancellationToken ct = default);
        Task<SalarySlipExportResultDto> GenerateSalarySlipFilesAsync(int accountId, string role, string? email, SalarySlipExportRequestDto dto, CancellationToken ct = default);
    }
}
