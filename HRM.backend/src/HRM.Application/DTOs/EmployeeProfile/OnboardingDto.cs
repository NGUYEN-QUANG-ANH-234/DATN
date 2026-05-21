namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class SubmitOnboardingDto
    {
        public int CandidateId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? TaxCode { get; set; }
        public string? SocialInsCode { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }

        // Thông tin bổ sung
        public string PhoneNumber { get; set; } = null!;
        public string PersonalEmail { get; set; } = null!;
        public string CurrentAddress { get; set; } = null!;
        public string PermanentAddress { get; set; } = null!;
        public string IdentityNumber { get; set; } = null!;

        // Thông tin khẩn cấp
        public string EmergencyContactName { get; set; } = null!;
        public string EmergencyPhone { get; set; } = null!;
        public string EmergencyRelation { get; set; } = null!;

        public IFormFile IdentityFrontFile { get; set; } = null!;
        public IFormFile IdentityBackFile { get; set; } = null!;
        public IFormFile? CertificateFile { get; set; }
    }

    public class ReviewOnboardingDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
        public int? RoleId { get; set; }
    }
}
