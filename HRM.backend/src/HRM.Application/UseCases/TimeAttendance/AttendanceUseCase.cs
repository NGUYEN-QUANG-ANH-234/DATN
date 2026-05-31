using System.Net;
using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.TimeAttendance
{
    public class AttendanceUseCase : IAttendanceUseCase
    {
        private const string CacheKey = "Attendance_Config_Cache";
        private const int MaxOpenSessionHours = 36;

        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IWorkShiftRepository _workShiftRepo;
        private readonly IConfigurationRepository _configRepo;
        private readonly IOvertimeRequestRepository _overtimeRepo;
        private readonly IOvertimeReconciliationService _overtimeReconciliationService;
        private readonly IAppCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public AttendanceUseCase(
            IEmployeeRepository employeeRepo,
            IAttendanceRepository attendanceRepo,
            IWorkShiftRepository workShiftRepo,
            IConfigurationRepository configRepo,
            IOvertimeRequestRepository overtimeRepo,
            IOvertimeReconciliationService overtimeReconciliationService,
            IAppCache cache,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _workShiftRepo = workShiftRepo;
            _configRepo = configRepo;
            _overtimeRepo = overtimeRepo;
            _overtimeReconciliationService = overtimeReconciliationService;
            _cache = cache;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<AttendanceTodayStatusDto> GetTodayStatusAsync(
            int accountId,
            CancellationToken ct = default)
        {
            var employee = await GetEmployeeByAccountAsync(accountId, ct);
            var now = DateTime.Now;
            var todayLog = await _attendanceRepo.GetOpenLogAsync(employee.Id, now, MaxOpenSessionHours, ct)
                ?? await _attendanceRepo.GetTodayLogAsync(employee.Id, now, ct);
            var shift = await GetEmployeeShiftAsync(employee, ct);

            var nextAction = todayLog == null
                ? "CHECK_IN"
                : todayLog.CheckOut.HasValue
                    ? "DONE"
                    : "CHECK_OUT";

            return new AttendanceTodayStatusDto
            {
                EmployeeName = employee.FullName,
                ShiftName = shift?.ShiftName,
                StartTime = FormatTime(shift?.StartTime),
                EndTime = FormatTime(shift?.EndTime),
                BreakStartTime = FormatTime(shift?.BreakStartTime),
                BreakEndTime = FormatTime(shift?.BreakEndTime),
                CheckIn = todayLog?.CheckIn,
                CheckOut = todayLog?.CheckOut,
                LateMinutes = CalculateLateMinutes(todayLog?.CheckIn, shift),
                EarlyLeaveMinutes = CalculateEarlyLeaveMinutes(todayLog?.CheckOut, shift),
                OvertimeMinutes = CalculateOvertimeMinutes(todayLog?.CheckOut, shift),
                NextAction = nextAction,
                Message = BuildTodayMessage(nextAction, shift)
            };
        }

        public async Task<AttendanceLogResponseDto> VerifyAndRecordAsync(
            int accountId,
            string clientIp,
            AttendanceGpsDto dto,
            CancellationToken ct = default)
        {
            var employee = await GetEmployeeByAccountAsync(accountId, ct);

            var config = await GetSecurityConfigAsync(ct);
            ValidateSecurity(clientIp, dto, config);

            return await _lockService.GetWithLockAsync(
                $"attendance_{employee.Id}",
                async (innerCt) =>
                    await VerifyAndRecordCoreAsync(employee, clientIp, dto, innerCt),
                cancellationToken: ct);
        }

        private async Task<AttendanceLogResponseDto> VerifyAndRecordCoreAsync(
            Employee employee,
            string clientIp,
            AttendanceGpsDto dto,
            CancellationToken ct)
        {
            var now = DateTime.Now;
            var todayLog = await _attendanceRepo.GetOpenLogAsync(employee.Id, now, MaxOpenSessionHours, ct)
                ?? await _attendanceRepo.GetTodayLogAsync(employee.Id, now, ct);
            var shift = await GetEmployeeShiftAsync(employee, ct);

            if (todayLog == null)
            {
                var log = new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    ShiftId = shift?.Id,
                    WorkDate = ResolveBusinessDate(now, shift),
                    CheckIn = now,
                    IpAddress = clientIp,
                    GpsLat = dto.Latitude,
                    GpsLong = dto.Longitude,
                    Status = ResolveCheckInStatus(now, shift)
                };

                await _attendanceRepo.InsertLogAsync(log);
                await _unitOfWork.CommitAsync(ct);

                return new AttendanceLogResponseDto
                {
                    Id = log.Id,
                    Action = "CHECK_IN",
                    CheckIn = log.CheckIn,
                    CheckOut = log.CheckOut,
                    IpAddress = clientIp,
                    Status = log.Status.ToString(),
                    LateMinutes = CalculateLateMinutes(log.CheckIn, shift),
                    Message = $"Chấm công vào thành công lúc {now:HH:mm}."
                };
            }

            if (todayLog.CheckOut.HasValue)
                throw new InvalidOperationException("Bạn đã hoàn tất chấm công hôm nay.");

            todayLog.CheckOut = now;
            todayLog.IpAddress = clientIp;
            todayLog.GpsLat = dto.Latitude;
            todayLog.GpsLong = dto.Longitude;
            todayLog.Status = ResolveCheckOutStatus(now, shift, todayLog.Status);
            await _attendanceRepo.UpdateAsync(todayLog, ct);
            await ReconcileOvertimeForLogAsync(todayLog, ct);
            await _unitOfWork.CommitAsync(ct);

            return new AttendanceLogResponseDto
            {
                Id = todayLog.Id,
                Action = "CHECK_OUT",
                CheckIn = todayLog.CheckIn,
                CheckOut = todayLog.CheckOut,
                IpAddress = clientIp,
                Status = todayLog.Status.ToString(),
                LateMinutes = CalculateLateMinutes(todayLog.CheckIn, shift),
                EarlyLeaveMinutes = CalculateEarlyLeaveMinutes(todayLog.CheckOut, shift),
                OvertimeMinutes = CalculateOvertimeMinutes(todayLog.CheckOut, shift),
                Message = $"Chấm công ra thành công lúc {now:HH:mm}."
            };
        }

        private async Task<Employee> GetEmployeeByAccountAsync(
            int accountId,
            CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null)
                throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân sự.");

            return employee;
        }

        private async Task<WorkShift?> GetEmployeeShiftAsync(Employee employee, CancellationToken ct)
        {
            if (!employee.DeptId.HasValue)
                return null;

            return await _cache.GetOrSetWithLockAsync(
                $"shift_config_dept_{employee.DeptId.Value}",
                async (innerCt) => await _workShiftRepo.GetByDeptIdAsync(employee.DeptId.Value, innerCt),
                TimeSpan.FromHours(12),
                _lockService,
                ct: ct);
        }

        private async Task<AttendanceConfigDto> GetSecurityConfigAsync(CancellationToken ct)
        {
            return await _cache.GetOrSetWithLockAsync(
                CacheKey,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchLatestConfigAsync(innerCt);
                    var rawConfig = configs.FirstOrDefault(c => c.ConfigGroup == "ATTENDANCE_PARAM")?.ParamValue;
                    if (string.IsNullOrWhiteSpace(rawConfig))
                        throw new InvalidOperationException("Chưa cấu hình tham số bảo mật chấm công.");

                    var config = JsonSerializer.Deserialize<AttendanceConfigDto>(rawConfig);
                    if (config == null)
                        throw new InvalidOperationException("Cấu hình chấm công không hợp lệ.");

                    return config;
                },
                TimeSpan.FromHours(2),
                _lockService,
                ct: ct);

#pragma warning disable CS0162
            var cachedConfig = await _cache.GetAsync<AttendanceConfigDto>(CacheKey);
            if (cachedConfig != null)
                return cachedConfig;

            var configs = await _configRepo.FetchLatestConfigAsync(ct);
            var rawConfig = configs.FirstOrDefault(c => c.ConfigGroup == "ATTENDANCE_PARAM")?.ParamValue;
            if (string.IsNullOrWhiteSpace(rawConfig))
                throw new InvalidOperationException("Chưa cấu hình tham số bảo mật chấm công.");

            var config = JsonSerializer.Deserialize<AttendanceConfigDto>(rawConfig);
            if (config == null)
                throw new InvalidOperationException("Cấu hình chấm công không hợp lệ.");

            await _cache.SetAsync(CacheKey, config, TimeSpan.FromHours(2), null, ct);
            return config;
#pragma warning restore CS0162
        }

        private static void ValidateSecurity(string clientIp, AttendanceGpsDto gpsDto, AttendanceConfigDto config)
        {
            var offices = GetConfiguredOffices(config).Where(x => x.IsActive).ToList();
            if (offices.Count == 0)
                throw new InvalidOperationException("Chưa cấu hình cơ sở chấm công.");

            if (!offices.Any(office => IsIpAllowed(clientIp, office.AllowedIpRanges)))
                throw new UnauthorizedAccessException("Bạn phải dùng mạng công ty để chấm công.");

            var lat = Convert.ToDouble(gpsDto.Latitude);
            var lng = Convert.ToDouble(gpsDto.Longitude);
            if (!offices.Any(office => IsWithinRadius(lat, lng, office.Latitude, office.Longitude, office.RadiusInMeters)))
                throw new UnauthorizedAccessException("Thiết bị không nằm trong vùng GPS của các cơ sở được phép chấm công.");
        }

        private static List<AttendanceOfficeLocationDto> GetConfiguredOffices(AttendanceConfigDto config)
        {
            if (config.OfficeLocations.Count > 0)
                return config.OfficeLocations;

            return new List<AttendanceOfficeLocationDto>
            {
                new()
                {
                    Name = "Cơ sở chính",
                    Latitude = config.Latitude,
                    Longitude = config.Longitude,
                    RadiusInMeters = config.RadiusInMeters,
                    AllowedIpRanges = config.AllowedIpRanges,
                    IsActive = true
                }
            };
        }

        private static bool IsIpAllowed(string clientIp, IEnumerable<string> allowedRanges)
        {
            if (!IPAddress.TryParse(clientIp, out var parsedClientIp))
                return false;

            parsedClientIp = NormalizeIp(parsedClientIp);

            return allowedRanges.Any(range => IsInRange(parsedClientIp, range));
        }

        private static bool IsInRange(IPAddress clientIp, string range)
        {
            var trimmed = range.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return false;

            if (!trimmed.Contains('/'))
                return IPAddress.TryParse(trimmed, out var exactIp) && NormalizeIp(exactIp).Equals(clientIp);

            var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkIp) || !int.TryParse(parts[1], out var prefixLength))
                return false;

            networkIp = NormalizeIp(networkIp);
            if (clientIp.AddressFamily != networkIp.AddressFamily || clientIp.AddressFamily != global::System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            if (prefixLength < 0 || prefixLength > 32)
                return false;

            var clientBytes = clientIp.GetAddressBytes();
            var networkBytes = networkIp.GetAddressBytes();
            var clientValue = BitConverter.ToUInt32(clientBytes.Reverse().ToArray(), 0);
            var networkValue = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);
            var mask = prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);

            return (clientValue & mask) == (networkValue & mask);
        }

        private static IPAddress NormalizeIp(IPAddress ipAddress)
        {
            if (IPAddress.IPv6Loopback.Equals(ipAddress))
                return IPAddress.Loopback;

            return ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;
        }

        private static bool IsWithinRadius(double lat, double lng, double officeLat, double officeLng, int radiusInMeters)
        {
            const double earthRadiusMeters = 6371000;

            static double ToRadians(double degrees) => degrees * Math.PI / 180d;

            var dLat = ToRadians(officeLat - lat);
            var dLng = ToRadians(officeLng - lng);
            var lat1 = ToRadians(lat);
            var lat2 = ToRadians(officeLat);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c <= radiusInMeters;
        }

        private static string? FormatTime(TimeSpan? value)
        {
            return value?.ToString(@"hh\:mm");
        }

        private static AttendanceStatus ResolveCheckInStatus(DateTime now, WorkShift? shift)
        {
            if (shift?.StartTime == null)
                return AttendanceStatus.Valid;

            var latestValidCheckIn = shift.StartTime.Value.Add(TimeSpan.FromMinutes(shift.LateThresholdMins));
            return now.TimeOfDay > latestValidCheckIn ? AttendanceStatus.Late : AttendanceStatus.Valid;
        }

        private static DateTime ResolveBusinessDate(DateTime now, WorkShift? shift)
        {
            if (shift?.StartTime == null)
                return now.Date;

            var shiftStartToday = now.Date.Add(shift.StartTime.Value);
            return now < shiftStartToday.AddHours(-6)
                ? now.Date.AddDays(-1)
                : now.Date;
        }

        private async Task ReconcileOvertimeForLogAsync(AttendanceLog log, CancellationToken ct)
        {
            if (!log.EmployeeId.HasValue || !log.CheckIn.HasValue || !log.CheckOut.HasValue)
                return;

            var requests = await _overtimeRepo.GetReconcileCandidatesAsync(
                log.EmployeeId.Value,
                log.CheckIn.Value,
                log.CheckOut.Value,
                ct);

            foreach (var request in requests)
            {
                await _overtimeReconciliationService.ReconcileAsync(request, log, ct);
                await _overtimeRepo.UpdateAsync(request, ct);
            }
        }

        private static AttendanceStatus ResolveCheckOutStatus(DateTime now, WorkShift? shift, AttendanceStatus currentStatus)
        {
            if (shift?.EndTime == null)
                return currentStatus;

            var earliestValidCheckOut = shift.EndTime.Value.Subtract(TimeSpan.FromMinutes(shift.EarlyLeaveThresholdMins));
            if (now.TimeOfDay < earliestValidCheckOut)
                return currentStatus == AttendanceStatus.Valid ? AttendanceStatus.Early : currentStatus;

            return currentStatus;
        }

        private static int CalculateLateMinutes(DateTime? checkIn, WorkShift? shift)
        {
            if (!checkIn.HasValue || shift?.StartTime == null)
                return 0;

            var latestValidCheckIn = shift.StartTime.Value.Add(TimeSpan.FromMinutes(shift.LateThresholdMins));
            return CalculateMinutesAfter(checkIn.Value.TimeOfDay, latestValidCheckIn);
        }

        private static int CalculateEarlyLeaveMinutes(DateTime? checkOut, WorkShift? shift)
        {
            if (!checkOut.HasValue || shift?.EndTime == null)
                return 0;

            var earliestValidCheckOut = shift.EndTime.Value.Subtract(TimeSpan.FromMinutes(shift.EarlyLeaveThresholdMins));
            return CalculateMinutesAfter(earliestValidCheckOut, checkOut.Value.TimeOfDay);
        }

        private static int CalculateOvertimeMinutes(DateTime? checkOut, WorkShift? shift)
        {
            if (!checkOut.HasValue || shift?.EndTime == null)
                return 0;

            return CalculateMinutesAfter(checkOut.Value.TimeOfDay, shift.EndTime.Value);
        }

        private static int CalculateMinutesAfter(TimeSpan later, TimeSpan earlier)
        {
            var minutes = (later - earlier).TotalMinutes;
            return minutes > 0 ? (int)Math.Ceiling(minutes) : 0;
        }

        private static string BuildTodayMessage(string nextAction, WorkShift? shift)
        {
            var shiftText = shift == null
                ? "Bạn chưa được gán ca làm việc theo phòng ban."
                : $"Ca {shift.ShiftName}: {FormatTime(shift.StartTime) ?? "--:--"} - {FormatTime(shift.EndTime) ?? "--:--"}.";

            return nextAction switch
            {
                "CHECK_IN" => $"{shiftText} Bạn chưa check-in hôm nay.",
                "CHECK_OUT" => $"{shiftText} Bạn đã check-in, hãy check-out khi kết thúc ca.",
                "DONE" => $"{shiftText} Bạn đã hoàn tất check-in/check-out hôm nay.",
                _ => shiftText
            };
        }
    }
}
