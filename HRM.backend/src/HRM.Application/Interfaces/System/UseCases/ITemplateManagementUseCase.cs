using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ITemplateManagementUseCase
    {
        Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken ct = default);
        Task<bool> UpdateTemplateAsync(TemplateDto dto, int adminId, CancellationToken ct = default);
    }
}
