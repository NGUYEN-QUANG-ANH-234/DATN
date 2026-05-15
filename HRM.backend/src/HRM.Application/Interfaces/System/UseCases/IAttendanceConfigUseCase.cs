using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IAttendanceConfigUseCase
    {
        Task<AttendanceConfigDto?> GetConfigAsync(CancellationToken ct = default);
        Task<bool> UpdateConfigAsync(AttendanceConfigDto dto, int adminId, CancellationToken ct = default);
    }
}
