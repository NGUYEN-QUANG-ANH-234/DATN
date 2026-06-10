using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class CompanyCalendarUseCase : ICompanyCalendarUseCase
    {
        private readonly ICompanyCalendarRepository _calendarRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;

        public CompanyCalendarUseCase(
            ICompanyCalendarRepository calendarRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork)
        {
            _calendarRepo = calendarRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CompanyCalendarDto>> GetByYearAsync(short year, CancellationToken ct = default)
        {
            ValidateYear(year);
            var calendars = await _calendarRepo.GetByYearAsync(year, ct);
            return calendars.Select(Map).ToList();
        }

        public async Task<CompanyCalendarDto> SaveAsync(short year, SaveCompanyCalendarDto dto, int actorAccountId, CancellationToken ct = default)
        {
            ValidateYear(year);
            Validate(dto, year);

            var effectiveFrom = dto.EffectiveFrom?.Date ?? new DateTime(year, 1, 1);
            var versionCode = string.IsNullOrWhiteSpace(dto.VersionCode)
                ? $"VN_COMPANY_CALENDAR_{year}_{DateTime.UtcNow:yyyyMMddHHmmss}"
                : dto.VersionCode.Trim();

            CompanyCalendar calendar;
            if (dto.Id.HasValue)
            {
                calendar = await _calendarRepo.GetByIdWithDaysAsync(dto.Id.Value, ct)
                    ?? throw new InvalidOperationException("Không tìm thấy lịch công ty cần cập nhật.");

                if (calendar.LockedAfterUsed)
                    throw new InvalidOperationException("Lịch đã được khóa sau khi sử dụng, hãy tạo phiên bản mới.");

                if (calendar.Year != year)
                    throw new ArgumentException("Năm lịch không khớp với dữ liệu cần lưu.");

                calendar.VersionCode = versionCode;
                calendar.EffectiveFrom = effectiveFrom;
                calendar.EffectiveTo = dto.EffectiveTo?.Date;
                calendar.Status = dto.Status;
                calendar.SourceRef = dto.SourceRef;
                calendar.Note = dto.Note;
                calendar.UpdatedAt = DateTime.UtcNow;
                calendar.UpdatedByAccountId = actorAccountId;
                calendar.Days.Clear();
            }
            else
            {
                calendar = new CompanyCalendar
                {
                    Year = year,
                    VersionCode = versionCode,
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = dto.EffectiveTo?.Date,
                    Status = dto.Status,
                    SourceRef = dto.SourceRef,
                    Note = dto.Note,
                    CreatedByAccountId = actorAccountId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedByAccountId = actorAccountId
                };
                await _calendarRepo.AddAsync(calendar, ct);
            }

            if (calendar.Status == PolicyVersionStatus.Active)
            {
                calendar.ActivatedAt ??= DateTime.UtcNow;
                await ArchiveOtherActiveVersionsAsync(year, calendar.Id, effectiveFrom, ct);
            }

            foreach (var day in dto.Days.OrderBy(d => d.Date.Date))
            {
                calendar.Days.Add(new CompanyCalendarDay
                {
                    Date = day.Date.Date,
                    DayType = day.DayType,
                    Name = day.Name.Trim(),
                    IsPaid = day.IsPaid,
                    IsOvertimeHoliday = day.IsOvertimeHoliday,
                    IsWorkingDayOverride = day.IsWorkingDayOverride,
                    Description = day.Description,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _auditLogRepo.LogSystemEventAsync(
                "UPSERT_COMPANY_CALENDAR",
                actorAccountId,
                "time_attendance",
                $"Cập nhật lịch nghỉ công ty năm {year}, phiên bản {calendar.VersionCode}");

            await _unitOfWork.CommitAsync(ct);
            return Map(calendar);
        }

        private async Task ArchiveOtherActiveVersionsAsync(short year, int currentCalendarId, DateTime effectiveFrom, CancellationToken ct)
        {
            var activeVersions = await _calendarRepo.FindAsync(
                c => c.Year == year &&
                     c.Status == PolicyVersionStatus.Active &&
                     (currentCalendarId == 0 || c.Id != currentCalendarId),
                ct);

            foreach (var version in activeVersions)
            {
                version.Status = PolicyVersionStatus.Archived;
                version.EffectiveTo = effectiveFrom.AddDays(-1);
                version.LockedAfterUsed = true;
                _calendarRepo.Update(version);
            }
        }

        private static void ValidateYear(short year)
        {
            if (year < 2000 || year > 2100)
                throw new ArgumentException("Năm lịch không hợp lệ.");
        }

        private static void Validate(SaveCompanyCalendarDto dto, short year)
        {
            var effectiveFrom = dto.EffectiveFrom?.Date ?? new DateTime(year, 1, 1);
            if (effectiveFrom.Year != year)
                throw new ArgumentException("Ngày hiệu lực phải thuộc năm đang cấu hình.");

            if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < effectiveFrom)
                throw new ArgumentException("Ngày hết hiệu lực phải sau ngày bắt đầu hiệu lực.");

            var duplicateDate = dto.Days
                .GroupBy(d => d.Date.Date)
                .FirstOrDefault(g => g.Count() > 1)
                ?.Key;
            if (duplicateDate.HasValue)
                throw new ArgumentException($"Ngày {duplicateDate:dd/MM/yyyy} đang bị nhập trùng.");

            foreach (var day in dto.Days)
            {
                if (day.Date.Year != year)
                    throw new ArgumentException("Tất cả ngày nghỉ phải thuộc đúng năm đang cấu hình.");

                if (string.IsNullOrWhiteSpace(day.Name))
                    throw new ArgumentException($"Ngày {day.Date:dd/MM/yyyy} chưa có tên sự kiện.");
            }
        }

        private static CompanyCalendarDto Map(CompanyCalendar calendar)
        {
            return new CompanyCalendarDto
            {
                Id = calendar.Id,
                Year = calendar.Year,
                VersionCode = calendar.VersionCode,
                EffectiveFrom = calendar.EffectiveFrom,
                EffectiveTo = calendar.EffectiveTo,
                Status = calendar.Status,
                SourceRef = calendar.SourceRef,
                LockedAfterUsed = calendar.LockedAfterUsed,
                Note = calendar.Note,
                Days = calendar.Days
                    .OrderBy(d => d.Date)
                    .Select(d => new CompanyCalendarDayDto
                    {
                        Id = d.Id,
                        Date = d.Date,
                        DayType = d.DayType,
                        Name = d.Name,
                        IsPaid = d.IsPaid,
                        IsOvertimeHoliday = d.IsOvertimeHoliday,
                        IsWorkingDayOverride = d.IsWorkingDayOverride,
                        Description = d.Description
                    })
                    .ToList()
            };
        }
    }
}
