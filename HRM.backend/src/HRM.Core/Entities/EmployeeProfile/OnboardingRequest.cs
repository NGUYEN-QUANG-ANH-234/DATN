using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums; // Import Enums mới
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("onboarding_requests")]
    public class OnboardingRequest
    {
        [Key] public int Id { get; set; }

        public int CandidateId { get; set; }
        [ForeignKey("CandidateId")] public virtual Candidate? Candidate { get; set; }

        [Column(TypeName = "json")]
        public required string RequestedDataJson { get; set; }

        // Dùng Enum thay vì string
        public OnboardingStatus Status { get; set; } = OnboardingStatus.Pending_HR;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RejectReason { get; set; }
    }
}