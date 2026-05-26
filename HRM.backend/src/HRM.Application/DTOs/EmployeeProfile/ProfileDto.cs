using System.ComponentModel.DataAnnotations;
using static Bogus.DataSets.Name;

namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class MyProfileDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? BirthDate { get; set; }

        // --- Thông tin liên hệ ---
        public string? PhoneNumber { get; set; }
        public string? PersonalEmail { get; set; }
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }

        // --- Định danh & Thuế & BHXH ---
        public string? IdentityNumber { get; set; }
        public string? TaxCode { get; set; }
        public string? SocialInsCode { get; set; }
        public string? SocialInsJoinDate { get; set; }
        public string? InsuranceHospital { get; set; }

        // --- Thông tin Ngân hàng ---
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }

        // --- Liên hệ khẩn cấp ---
        public string? EmergencyContactName { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? EmergencyRelation { get; set; }

        public string? JoinedDate { get; set; }
        public string? AvatarUrl { get; set; }
        public string? IdentityFrontUrl { get; set; }
        public string? IdentityBackUrl { get; set; }
        public string? CertificateUrl { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ProfileUpdateRequestDto
    {
        // --- Thông tin cá nhân cơ bản ---
        [StringLength(100)] public string? FullName { get; set; }
        public Gender? Gender { get; set; }
        public DateTime? BirthDate { get; set; }

        // --- Thông tin liên hệ ---
        [StringLength(20)] public string? PhoneNumber { get; set; }
        [StringLength(100)] public string? PersonalEmail { get; set; }
        [StringLength(255)] public string? CurrentAddress { get; set; }
        [StringLength(255)] public string? PermanentAddress { get; set; }

        // --- Định danh & Thuế & BHXH ---
        [StringLength(20)] public string? IdentityNumber { get; set; }
        [StringLength(20)] public string? TaxCode { get; set; }
        [StringLength(20)] public string? SocialInsCode { get; set; }
        public DateTime? SocialInsJoinDate { get; set; }
        [StringLength(100)] public string? InsuranceHospital { get; set; }

        // --- Thông tin Ngân hàng ---
        [StringLength(50)] public string? BankAccount { get; set; }
        [StringLength(100)] public string? BankName { get; set; }

        // --- Liên hệ khẩn cấp ---
        [StringLength(100)] public string? EmergencyContactName { get; set; }
        [StringLength(20)] public string? EmergencyPhone { get; set; }
        [StringLength(50)] public string? EmergencyRelation { get; set; }

        // --- File Uploads (Minh chứng) ---
        public IFormFile? AvatarFile { get; set; }
        public IFormFile? IdentityFrontFile { get; set; }
        public IFormFile? IdentityBackFile { get; set; }
        public IFormFile? CertificateFile { get; set; }
    }

    public class MyContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal SalaryPercentage { get; set; }
        public decimal InsuranceSalary { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Version { get; set; }
        public string? NegotiationNote { get; set; }
    }   

    public class ReviewProfileUpdateDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }

    public class PendingProfileRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string RequestedDataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime DeadlineSLA { get; set; }
    }
}
