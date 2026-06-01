using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // --- Thông tin cá nhân & Địa chỉ ---
        public Gender? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        [StringLength(20)] public string? PhoneNumber { get; set; }
        [StringLength(100)] public string? PersonalEmail { get; set; }
        [StringLength(255)] public string? CurrentAddress { get; set; }
        [StringLength(255)] public string? PermanentAddress { get; set; }

        // --- Định danh, Thuế & BHXH ---
        [StringLength(20)] public string? IdentityNumber { get; set; }
        [StringLength(20)] public string? TaxCode { get; set; }
        [StringLength(20)] public string? SocialInsCode { get; set; }
        public DateTime? SocialInsJoinDate { get; set; }
        [StringLength(100)] public string? InsuranceHospital { get; set; }

        // --- Thông tin Ngân hàng ---
        [StringLength(50)] public string? BankAccount { get; set; }
        [StringLength(100)] public string? BankName { get; set; }

        // --- Thông tin khẩn cấp ---
        [StringLength(100)] public string? EmergencyContactName { get; set; }
        [StringLength(20)] public string? EmergencyPhone { get; set; }
        [StringLength(50)] public string? EmergencyRelation { get; set; }

        // --- Tổ chức & Trạng thái ---
        public int? DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }
        public int? PositionId { get; set; }
        [ForeignKey("PositionId")] public virtual Position? Position { get; set; }
        public int? JobLevelId { get; set; }
        [ForeignKey("JobLevelId")] public virtual JobLevel? JobLevel { get; set; }
        public int? ManagerId { get; set; }
        [ForeignKey("ManagerId")] public virtual Employee? Manager { get; set; }

        public EmployeeType Type { get; set; } = EmployeeType.Probation;
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Probation;
        public ResidenceStatus ResidenceStatus { get; set; } = ResidenceStatus.Resident;
        public TaxCodeStatus TaxCodeStatus { get; set; } = TaxCodeStatus.Unknown;
        public DateTime? JoinedDate { get; set; }

        // --- Hồ sơ giấy tờ ---
        [StringLength(500)] public string? AvatarUrl { get; set; }
        [StringLength(500)] public string? IdentityFrontUrl { get; set; }
        [StringLength(500)] public string? IdentityBackUrl { get; set; }
        [StringLength(500)] public string? CertificateUrl { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<Dependent> Dependents { get; set; } = new List<Dependent>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
