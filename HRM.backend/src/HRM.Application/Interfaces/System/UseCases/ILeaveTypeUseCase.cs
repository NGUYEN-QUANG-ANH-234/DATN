using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ILeaveTypeUseCase
    {
        Task<List<LeaveTypeSelectDto>> GetLeaveTypesForSelectAsync(CancellationToken ct = default);
    }
}
