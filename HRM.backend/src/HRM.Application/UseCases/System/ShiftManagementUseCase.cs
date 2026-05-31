using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class ShiftManagementUseCase : IShiftManagementUseCase
    {
        private readonly IWorkShiftRepository _shiftRepo;
        private readonly IWorkCalendarConfigRepository _calendarRepo;
        private readonly ILeaveBalanceRepository _leaveRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IAppCache _appCache;

        public ShiftManagementUseCase(
            IWorkShiftRepository shiftRepo,
            IWorkCalendarConfigRepository calendarRepo,
            ILeaveBalanceRepository leaveRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IAppCache appCache)
        {
            _shiftRepo = shiftRepo;
            _calendarRepo = calendarRepo;
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
            if (dto.LateThresholdMins < 0 || dto.EarlyLeaveThresholdMins < 0)
            {
                throw new ArgumentException("Ngưỡng đi muộn/về sớm không được nhỏ hơn 0.");
            }
            if (dto.Month < 1 || dto.Month > 12)
            {
                throw new ArgumentException("Tháng cấu hình kỳ công không hợp lệ.");
            }
            if (dto.StandardWorkDays <= 0 || dto.StandardWorkDays > 31)
            {
                throw new ArgumentException("Ngày công chuẩn phải lớn hơn 0 và không vượt quá 31.");
            }
            if (dto.StandardHoursPerDay <= 0 || dto.StandardHoursPerDay > 24)
            {
                throw new ArgumentException("Số giờ chuẩn/ngày phải lớn hơn 0 và không vượt quá 24.");
            }
            if (dto.TotalDays < 0)
            {
                throw new ArgumentException("Số ngày phép định biên không được nhỏ hơn 0.");
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

                var calendar = await _calendarRepo.GetByDeptPeriodAsync(dto.DeptId, dto.Month, dto.Year, innerCt);
                if (calendar?.IsLocked == true)
                {
                    throw new InvalidOperationException("Kỳ công đã khóa, không thể cập nhật cấu hình ngày công chuẩn.");
                }

                if (calendar == null)
                {
                    calendar = new WorkCalendarConfig
                    {
                        DeptId = dto.DeptId,
                        Month = dto.Month,
                        Year = dto.Year,
                        StandardWorkDays = dto.StandardWorkDays,
                        StandardHoursPerDay = dto.StandardHoursPerDay,
                        IncludePaidLeaveInWorkDays = dto.IncludePaidLeaveInWorkDays,
                        WorkingDaysOfWeek = dto.WorkingDaysOfWeek,
                        HolidayDatesJson = dto.HolidayDatesJson,
                        IsLocked = dto.LockWorkCalendar,
                        Note = dto.CalendarNote,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedByAccountId = actorId
                    };
                    await _calendarRepo.AddAsync(calendar, innerCt);
                }
                else
                {
                    calendar.StandardWorkDays = dto.StandardWorkDays;
                    calendar.StandardHoursPerDay = dto.StandardHoursPerDay;
                    calendar.IncludePaidLeaveInWorkDays = dto.IncludePaidLeaveInWorkDays;
                    calendar.WorkingDaysOfWeek = dto.WorkingDaysOfWeek;
                    calendar.HolidayDatesJson = dto.HolidayDatesJson;
                    calendar.IsLocked = dto.LockWorkCalendar;
                    calendar.Note = dto.CalendarNote;
                    calendar.UpdatedAt = DateTime.UtcNow;
                    calendar.UpdatedByAccountId = actorId;
                }

                await _leaveRepo.UpdateDeptAllocatedDaysAsync(dto.DeptId, dto.LeaveTypeId, dto.Year, dto.TotalDays, innerCt);
                await _auditLogRepo.LogSystemEventAsync("UPDATE_DEPT_SCHEDULE", actorId, "time_attendance", $"Thiết lập ca '{dto.ShiftName}', kỳ công {dto.Month:D2}/{dto.Year}, {dto.StandardWorkDays} ngày công chuẩn cho phòng ban ID: {dto.DeptId}");
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
            var calendarConfigs = await _calendarRepo.GetAllWithDepartmentAsync(ct);

            var result = new List<ConfiguredScheduleDto>();

            foreach (var shift in shifts)
            {
                if (shift.DeptId == null || shift.Department == null) continue;

                // Tìm quỹ phép tương ứng của phòng ban này
                var matchedLeave = leaveConfigs.FirstOrDefault(l => l.DeptId == shift.DeptId);
                var matchedCalendar = calendarConfigs.FirstOrDefault(c => c.DeptId == shift.DeptId);

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
                    TotalDays = matchedLeave?.TotalDays ?? 0,
                    Month = matchedCalendar?.Month,
                    StandardWorkDays = matchedCalendar?.StandardWorkDays,
                    StandardHoursPerDay = matchedCalendar?.StandardHoursPerDay,
                    IncludePaidLeaveInWorkDays = matchedCalendar?.IncludePaidLeaveInWorkDays ?? true,
                    WorkingDaysOfWeek = matchedCalendar?.WorkingDaysOfWeek,
                    HolidayDatesJson = matchedCalendar?.HolidayDatesJson,
                    IsWorkCalendarLocked = matchedCalendar?.IsLocked ?? false,
                    CalendarNote = matchedCalendar?.Note
                });
            }

            return result;
        }

        public async Task<List<ScheduleChangeHistoryDto>> GetScheduleChangeHistoryAsync(CancellationToken ct = default)
        {
            var logs = await _auditLogRepo.FetchLogsWithDetailAsync(null, "time_attendance", DateTime.UtcNow.AddDays(-90), null, ct);
            return logs
                .Where(x => x.ActionType == "UPDATE_DEPT_SCHEDULE")
                .OrderByDescending(x => x.Timestamp)
                .Take(30)
                .Select(x => new ScheduleChangeHistoryDto
                {
                    Id = x.Id,
                    ActionType = x.ActionType ?? string.Empty,
                    ActorName = x.Account?.FullName ?? x.Account?.Email,
                    Message = x.NewValues,
                    Timestamp = x.Timestamp
                })
                .ToList();
        }

    }
}
