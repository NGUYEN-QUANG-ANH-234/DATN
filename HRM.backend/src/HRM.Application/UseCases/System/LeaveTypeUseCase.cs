using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class LeaveTypeUseCase : ILeaveTypeUseCase
    {
        private const string CacheKey = "leave_types_active";

        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        public LeaveTypeUseCase(ILeaveTypeRepository leaveTypeRepo, IAppCache cache, ILockService lockService)
        {
            _leaveTypeRepo = leaveTypeRepo;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<List<LeaveTypeSelectDto>> GetLeaveTypesForSelectAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CacheKey,
                async (innerCt) =>
                {
                    var list = await _leaveTypeRepo.GetAllAsync(innerCt);
                    return list.Select(x => new LeaveTypeSelectDto
                    {
                        Id = x.Id,
                        TypeName = x.TypeName ?? string.Empty,
                        Category = x.Category.ToString(),
                        IsPaid = x.IsPaid,
                        CountsAsUnpaidForInsurance = x.CountsAsUnpaidForInsurance,
                        CountsAsWorkday = x.CountsAsWorkday,
                        DeductAnnualLeave = x.DeductAnnualLeave,
                        AffectsKpiPenalty = x.AffectsKpiPenalty
                    }).ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }
    }
}
