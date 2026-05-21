using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class LeaveTypeUseCase : ILeaveTypeUseCase
    {
        private readonly ILeaveTypeRepository _leaveTypeRepo;

        public LeaveTypeUseCase(ILeaveTypeRepository leaveTypeRepo)
        {
            _leaveTypeRepo = leaveTypeRepo;
        }

        public async Task<List<LeaveTypeSelectDto>> GetLeaveTypesForSelectAsync(CancellationToken ct = default)
        {
            var list = await _leaveTypeRepo.GetAllAsync(ct);
            return list.Select(x => new LeaveTypeSelectDto
            {
                Id = x.Id,
                TypeName = x.TypeName ?? string.Empty
            }).ToList();
        }
    }
}
