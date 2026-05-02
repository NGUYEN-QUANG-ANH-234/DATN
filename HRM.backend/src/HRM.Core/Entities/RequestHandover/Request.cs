using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;

namespace HRM.backend.src.HRM.Core.Entities.RequestHandover
{
    [Table("requests")]
    public class Request
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public RequestType RequestType { get; set; }

        public int? TargetDeptId { get; set; }
         [ForeignKey("TargetDeptId")] public virtual Department? TargetDepartment { get; set; }

        public int? TargetPositionId { get; set; }
         [ForeignKey("TargetPositionId")] public virtual Position? TargetPosition { get; set; }

        public string? Content { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public int? CurrentApproverId { get; set; }
        public DateTime? DeadlineAt { get; set; }

         //Navigation Property: 1 Request có thể phát sinh nhiều HandoverRequest
        public virtual ICollection<HandoverRequest> HandoverRequests { get; set; } = new List<HandoverRequest>();
    }
}