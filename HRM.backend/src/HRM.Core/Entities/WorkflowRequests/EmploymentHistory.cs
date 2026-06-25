using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.WorkflowRequests
{
    [Table("employment_history")]
    public class EmploymentHistory
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public HistoryType Type { get; set; }

        // Kiểu TEXT trong DB, không nên giới hạn cứng StringLength
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        // Thêm trường Ngày hiệu lực (Dùng cho nghiệp vụ và bộ lọc)
        public DateTime EffectiveDate { get; set; }

        // Ngày thao tác (Dùng để audit hệ thống)
        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;

        public int? ApprovedBy { get; set; }
         [ForeignKey("ApprovedBy")] public virtual Employee? Approver { get; set; }
    }
}
