namespace HRM.backend.src.HRM.Application.DTOs.TimeAttendance
{
    public class AttendanceGpsDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class AttendanceLogResponseDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class AttendanceNetworkInfoDto
    {
        public string ClientIp { get; set; } = string.Empty;
        public string SuggestedCidr { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class AttendanceTodayStatusDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string? ShiftName { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? BreakStartTime { get; set; }
        public string? BreakEndTime { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string NextAction { get; set; } = "CHECK_IN";
        public string Message { get; set; } = string.Empty;
    }
}
