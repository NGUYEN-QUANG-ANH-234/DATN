using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ISalaryVariableUseCase
    {
        Task<IEnumerable<VariableDto>> GetAllVariablesAsync(CancellationToken ct = default);
        Task<bool> DefineVariableAsync(VariableDto dto, int adminId, CancellationToken ct = default);
        Task<bool> SetVariableActiveAsync(string code, bool isActive, int adminId, CancellationToken ct = default);
    }
}
