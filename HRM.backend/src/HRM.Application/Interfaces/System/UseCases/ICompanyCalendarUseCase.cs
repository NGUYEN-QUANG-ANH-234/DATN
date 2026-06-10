using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ICompanyCalendarUseCase
    {
        Task<List<CompanyCalendarDto>> GetByYearAsync(short year, CancellationToken ct = default);
        Task<CompanyCalendarDto> SaveAsync(short year, SaveCompanyCalendarDto dto, int actorAccountId, CancellationToken ct = default);
    }
}
