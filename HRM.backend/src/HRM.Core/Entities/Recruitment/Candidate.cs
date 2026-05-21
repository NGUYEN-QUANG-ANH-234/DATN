using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.Recruitment
{
    [Table("candidates")]
    public class Candidate
    {
        [Key] public int Id { get; set; }

        // Khóa ngoại liên kết tới bảng recruitment_requests
        public int? RecruitmentRequestId { get; set; }
        [ForeignKey("RecruitmentRequestId")]
        public virtual RecruitmentRequest? RecruitmentRequest { get; set; }

        [StringLength(100)]
        public required string FullName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? TrackingCode { get; set; }

        [StringLength(255)]
        public string? CvFilePath { get; set; }

        public CandidateStatus Status { get; set; } = CandidateStatus.New;

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow.Date;
    }
}