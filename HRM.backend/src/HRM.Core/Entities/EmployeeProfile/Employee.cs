using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.Organization;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("employees")]
    public class Employee
    {
        [Key] public int Id { get; set; }

        public int? AccountId { get; set; }
         [ForeignKey("AccountId")] public virtual Account? Account { get; set; }

        public int? CandidateId { get; set; }
         [ForeignKey("CandidateId")] public virtual Candidate? Candidate { get; set; }

        [StringLength(20)] public required string EmployeeCode { get; set; }
        [StringLength(100)] public required string FullName { get; set; }

        public Gender? Gender { get; set; }
        public DateTime? BirthDate { get; set; }

        [StringLength(20)] public string? IdentityNumber { get; set; }
        [StringLength(20)] public string? TaxCode { get; set; }
        [StringLength(20)] public string? SocialInsCode { get; set; }

        [StringLength(50)] public string? BankAccount { get; set; }
        [StringLength(100)] public string? BankName { get; set; }

        public int? DeptId { get; set; }
         [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int? PositionId { get; set; }
         [ForeignKey("PositionId")] public virtual Position? Position { get; set; }

        public bool IsIntern { get; set; } = false;
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Probation;
        public DateTime? JoinedDate { get; set; }

        // --- Navigation Properties (Quan hệ 1-N) ---
        public virtual ICollection<Dependent> Dependents { get; set; } = new List<Dependent>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}