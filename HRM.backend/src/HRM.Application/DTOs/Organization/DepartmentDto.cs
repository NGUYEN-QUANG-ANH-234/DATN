using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.Organization
{
    public class DepartmentTreeDto
    {
        public int Id { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public int? ParentDeptId { get; set; }
        public int? ManagerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<DepartmentTreeDto> Children { get; set; } = new List<DepartmentTreeDto>();
    }

    public class UpdateDeptStructureDto
    {
        public int? NewParentId { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required, StringLength(20)]
        public string DeptCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string DeptName { get; set; } = string.Empty;

        public int? ParentDeptId { get; set; }
    }

    public class ConfiguredScheduleDto
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;

        // Thông tin Ca
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public int LateThresholdMins { get; set; }
        public int EarlyLeaveThresholdMins { get; set; }

        // Thông tin Quỹ phép
        public string LeaveTypeName { get; set; } = string.Empty;
        public short Year { get; set; }
        public decimal TotalDays { get; set; }
    }

    public class DeptLeaveConfigDto
    {
        public int DeptId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public short Year { get; set; }
        public decimal TotalDays { get; set; }
    }
}
