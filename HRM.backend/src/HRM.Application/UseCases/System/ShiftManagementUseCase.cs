using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class ShiftManagementUseCase : IShiftManagementUseCase
    {
        private readonly IWorkShiftRepository _shiftRepo;
        private readonly ILeaveBalanceRepository _leaveRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IAppCache _appCache;

        public ShiftManagementUseCase(
            IWorkShiftRepository shiftRepo,
            ILeaveBalanceRepository leaveRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IAppCache appCache)
        {
            _shiftRepo = shiftRepo;
            _leaveRepo = leaveRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _appCache = appCache;
        }

        public async Task<bool> ConfigureWorkScheduleAsync(ConfigureWorkScheduleDto dto, int actorId, CancellationToken ct = default)
        {
            // 1. Fail-fast: validateTimeRange()
            if (dto.EndTime <= dto.StartTime)
            {
                throw new ArgumentException("Giờ kết thúc ca phải lớn hơn giờ bắt đầu ca.");
            }

            if (dto.BreakStartTime.HasValue && dto.BreakEndTime.HasValue)
            {
                if (dto.BreakEndTime <= dto.BreakStartTime || dto.BreakStartTime < dto.StartTime || dto.BreakEndTime > dto.EndTime)
                {
                    throw new ArgumentException("Khung giờ nghỉ trưa không hợp lệ hoặc nằm ngoài giờ làm việc.");
                }
            }

            // Khóa luồng cấu hình lịch trình theo Phòng ban để tránh xung đột Transaction khi Admin thao tác cùng lúc
            return await _lockService.GetWithLockAsync($"config_schedule_dept_{dto.DeptId}", async (innerCt) =>
            {
                // ĐÃ SỬA: Tìm Ca làm việc theo DeptId thay vì ShiftName
                var existingShift = await _shiftRepo.GetByDeptIdAsync(dto.DeptId, innerCt);
                if (existingShift != null)
                {
                    existingShift.ShiftName = dto.ShiftName; // Cập nhật cả tên ca
                    existingShift.StartTime = dto.StartTime;
                    existingShift.EndTime = dto.EndTime;
                    existingShift.BreakStartTime = dto.BreakStartTime;
                    existingShift.BreakEndTime = dto.BreakEndTime;
                    existingShift.LateThresholdMins = dto.LateThresholdMins;
                    existingShift.EarlyLeaveThresholdMins = dto.EarlyLeaveThresholdMins;
                }
                else
                {
                    var newShift = new WorkShift
                    {
                        DeptId = dto.DeptId, // BẮT BUỘC GÁN
                        ShiftName = dto.ShiftName,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BreakStartTime = dto.BreakStartTime,
                        BreakEndTime = dto.BreakEndTime,
                        LateThresholdMins = dto.LateThresholdMins,
                        EarlyLeaveThresholdMins = dto.EarlyLeaveThresholdMins,
                        IsActive = true
                    };
                    await _shiftRepo.AddAsync(newShift, innerCt);
                }

                await _leaveRepo.UpdateDeptAllocatedDaysAsync(dto.DeptId, dto.LeaveTypeId, dto.Year, dto.TotalDays, innerCt);
                await _auditLogRepo.LogSystemEventAsync("UPDATE_DEPT_SCHEDULE", actorId, "time_attendance", $"Thiết lập Ca làm việc '{dto.ShiftName}' cho Phòng ban ID: {dto.DeptId}");
                await _unitOfWork.CommitAsync(innerCt);

                await _appCache.RemoveAsync($"shift_config_dept_{dto.DeptId}", innerCt);

                return true;
            }, TimeSpan.FromSeconds(10), ct);
        }

        public async Task<List<ConfiguredScheduleDto>> GetConfiguredSchedulesAsync(CancellationToken ct = default)
        {
            // Lấy độc lập 2 luồng dữ liệu
            var shifts = await _shiftRepo.GetAllActiveWithDepartmentAsync(ct);
            var leaveConfigs = await _leaveRepo.GetDeptLeaveConfigsAsync(ct);

            var result = new List<ConfiguredScheduleDto>();

            foreach (var shift in shifts)
            {
                if (shift.DeptId == null || shift.Department == null) continue;

                // Tìm quỹ phép tương ứng của phòng ban này
                var matchedLeave = leaveConfigs.FirstOrDefault(l => l.DeptId == shift.DeptId);

                result.Add(new ConfiguredScheduleDto
                {
                    DeptId = shift.DeptId.Value,
                    DeptName = shift.Department.DeptName,
                    ShiftName = shift.ShiftName,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    BreakStartTime = shift.BreakStartTime,
                    BreakEndTime = shift.BreakEndTime,
                    LateThresholdMins = shift.LateThresholdMins,
                    EarlyLeaveThresholdMins = shift.EarlyLeaveThresholdMins,
                    LeaveTypeName = matchedLeave?.LeaveTypeName ?? "Chưa cấu hình",
                    Year = matchedLeave?.Year ?? (short)DateTime.UtcNow.Year,
                    TotalDays = matchedLeave?.TotalDays ?? 0
                });
            }

            return result;
        }

    }
}
