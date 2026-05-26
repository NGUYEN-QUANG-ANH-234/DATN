using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class VariableDto
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Mã biến không được chứa ký tự đặc biệt")]
        public required string Code { get; set; }

        [Required]
        public required string Source { get; set; } // Ví dụ: Attendance.OT_Total

        public string? Description { get; set; }
    }

    public class SlaDto
    {
        public required string ModuleCode { get; set; } // VD: PAYROLL, LEAVE, DISCIPLINE
        public required string Value { get; set; }      // VD: "48", "3" (Lưu chuỗi cho linh hoạt)
        public required string Unit { get; set; }       // VD: HOURS, DAYS
    }

    public class TemplateDto
    {
        public required string TemplateKey { get; set; } // VD: PROMOTION, NEW_TASK, SLA_WARNING
        public required string Subject { get; set; }     // Tiêu đề Email/Thông báo
        public required string BodyHtml { get; set; }    // Nội dung chứa các biến {name}, {position}...
    }

    public class AttendanceConfigDto
    {
        // Backward-compatible fields for old ATTENDANCE_CONFIG rows.
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusInMeters { get; set; }
        public List<string> AllowedIpRanges { get; set; } = new();
        public List<AttendanceOfficeLocationDto> OfficeLocations { get; set; } = new();
    }

    public class AttendanceOfficeLocationDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusInMeters { get; set; }
        public List<string> AllowedIpRanges { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
