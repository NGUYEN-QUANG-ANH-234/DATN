using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("profile_update_requests")]
    public class ProfileUpdateRequest
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        // Lưu toàn bộ dữ liệu thay đổi dưới dạng JSON để HR so sánh (Before/After)
        [Column(TypeName = "json")] // Hoặc nvarchar(max) nếu dùng SQL Server
        public required string RequestedDataJson { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending_HR; // Enum: PendingHR, Approved, Rejected, Auto_Approved_SLA

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime DeadlineSLA { get; set; } // Hạn chót để HR duyệt (VD: CreatedAt + 72h)

        [StringLength(500)] public string? RejectReason { get; set; }
    }
}