using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface IShiftManagementUseCase
    {
        Task<bool> ConfigureWorkScheduleAsync(ConfigureWorkScheduleDto dto, int actorId, CancellationToken ct = default);
        Task<List<ConfiguredScheduleDto>> GetConfiguredSchedulesAsync(CancellationToken ct = default);
        Task<List<ScheduleChangeHistoryDto>> GetScheduleChangeHistoryAsync(CancellationToken ct = default);
    }
}
