using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("dependent_update_requests")]
    public class DependentUpdateRequest
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? DependentId { get; set; }
        [ForeignKey("DependentId")] public virtual Dependent? Dependent { get; set; }

        [StringLength(30)]
        public required string ActionType { get; set; }

        public required string RequestedDataJson { get; set; }
        public string? EvidenceUrl { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending_HR;
        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewerAccountId { get; set; }
    }
}
