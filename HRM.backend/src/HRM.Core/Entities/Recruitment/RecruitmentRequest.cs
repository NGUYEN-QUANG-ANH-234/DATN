using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.Organization; // Mở comment nếu Department, Position nằm ở thư mục Organization

namespace HRM.backend.src.HRM.Core.Entities.Recruitment
{
    [Table("recruitment_requests")]
    public class RecruitmentRequest
    {
        [Key] public int Id { get; set; }

        public int? DeptId { get; set; }
         [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int? PositionId { get; set; }
         [ForeignKey("PositionId")] public virtual Position? Position { get; set; }

        public int Quantity { get; set; } = 1;

        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public RecruitmentRequestStatus Status { get; set; } = RecruitmentRequestStatus.PendingHR;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties (Quan hệ 1-N) ---
        // Một Yêu cầu tuyển dụng (Job Posting) có thể có nhiều Ứng viên nộp CV vào
        public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    }
}