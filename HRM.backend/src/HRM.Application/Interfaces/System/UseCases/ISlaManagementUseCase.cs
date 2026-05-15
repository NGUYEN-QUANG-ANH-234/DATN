using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ISlaManagementUseCase
    {
        Task<IEnumerable<SlaDto>> GetSLAConfigsAsync(CancellationToken ct = default);
        Task<bool> UpdateSLAParameterAsync(SlaDto dto, int adminId, CancellationToken ct = default);
    }
}
