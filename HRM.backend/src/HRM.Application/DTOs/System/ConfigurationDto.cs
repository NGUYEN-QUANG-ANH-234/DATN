using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class VariableDto
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Mã biến không được chứa ký tự đặc biệt")]
        public required string Code { get; set; }

        [Required]
        public required string Source { get; set; } // Ví dụ: overtime_hours

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SalaryVariableStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class SlaDto
    {
        public required string ModuleCode { get; set; } // VD: PAYROLL, LEAVE, DISCIPLINE
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public required string Value { get; set; }      // VD: "48", "3" (Lưu chuỗi cho linh hoạt)
        public required string Unit { get; set; }       // VD: HOURS, DAYS
        public bool IsActive { get; set; } = true;
    }

    public class SlaStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class TemplateDto
    {
        public required string TemplateKey { get; set; } // VD: PROMOTION, NEW_TASK, SLA_WARNING
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> AllowedPlaceholders { get; set; } = new();
        public List<string> SystemPlaceholders { get; set; } = new();
        public List<TemplateVariableDto> CustomVariables { get; set; } = new();
        public required string Subject { get; set; }     // Tiêu đề Email/Thông báo
        public required string BodyHtml { get; set; }    // Nội dung chứa các biến {name}, {position}...
    }

    public class TemplateVariableDto
    {
        [Required]
        [RegularExpression(@"^[a-z][a-z0-9_]{1,49}$", ErrorMessage = "Mã biến phải dùng chữ thường, số, gạch dưới và bắt đầu bằng chữ.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Label { get; set; } = string.Empty;

        public string DataType { get; set; } = "Text";
        public string SourceType { get; set; } = "Manual";
        public bool IsRequired { get; set; }
        public string? Description { get; set; }
        public string Placeholder => $"{{{Code}}}";
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
