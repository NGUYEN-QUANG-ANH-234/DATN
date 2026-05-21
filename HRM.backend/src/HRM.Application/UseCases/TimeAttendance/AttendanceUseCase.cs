using System.Net;
using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
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

        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IWorkShiftRepository _workShiftRepo;
        private readonly IConfigurationRepository _configRepo;
        private readonly IAppCache _cache;
        private readonly IUnitOfWork _unitOfWork;

        public AttendanceUseCase(
            IEmployeeRepository employeeRepo,
            IAttendanceRepository attendanceRepo,
            IWorkShiftRepository workShiftRepo,
            IConfigurationRepository configRepo,
            IAppCache cache,
            IUnitOfWork unitOfWork)
        {
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _workShiftRepo = workShiftRepo;
            _configRepo = configRepo;
            _cache = cache;
            _unitOfWork = unitOfWork;
        }

        public async Task<AttendanceTodayStatusDto> GetTodayStatusAsync(
            int accountId,
            CancellationToken ct = default)
        {
            var employee = await GetEmployeeByAccountAsync(accountId, ct);
            var now = DateTime.Now;
            var todayLog = await _attendanceRepo.GetTodayLogAsync(employee.Id, now, ct);
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
            ValidateSecurity(clientIp, config);

            var now = DateTime.Now;
            var todayLog = await _attendanceRepo.GetTodayLogAsync(employee.Id, now, ct);
            var shift = await GetEmployeeShiftAsync(employee, ct);

            if (todayLog == null)
            {
                var log = new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    ShiftId = shift?.Id,
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
            await _unitOfWork.CommitAsync(ct);

            return new AttendanceLogResponseDto
            {
                Id = todayLog.Id,
                Action = "CHECK_OUT",
                CheckIn = todayLog.CheckIn,
                CheckOut = todayLog.CheckOut,
                IpAddress = clientIp,
                Status = todayLog.Status.ToString(),
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
            return employee.DeptId.HasValue
                ? await _workShiftRepo.GetByDeptIdAsync(employee.DeptId.Value, ct)
                : null;
        }

        private async Task<AttendanceConfigDto> GetSecurityConfigAsync(CancellationToken ct)
        {
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
        }

        private static void ValidateSecurity(string clientIp, AttendanceConfigDto config)
        {
            if (config.AllowedIpRanges.Count == 0)
                throw new InvalidOperationException("Chưa cấu hình dải IP văn phòng cho chấm công.");

            if (!IsIpAllowed(clientIp, config.AllowedIpRanges))
                throw new UnauthorizedAccessException("Bạn phải dùng mạng công ty để chấm công.");
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

        private static AttendanceStatus ResolveCheckOutStatus(DateTime now, WorkShift? shift, AttendanceStatus currentStatus)
        {
            if (shift?.EndTime == null)
                return currentStatus;

            var earliestValidCheckOut = shift.EndTime.Value.Subtract(TimeSpan.FromMinutes(shift.EarlyLeaveThresholdMins));
            if (now.TimeOfDay < earliestValidCheckOut)
                return AttendanceStatus.Early;

            return currentStatus;
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
