using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("contract_legal_snapshots")]
    public class ContractLegalSnapshot
    {
        [Key] public int Id { get; set; }

        public int ContractId { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual Contract Contract { get; set; } = null!;

        public int Version { get; set; } = 1;
        public ContractLegalDocumentType? LegalDocumentType { get; set; }

        [StringLength(80)] public string? LegalDocumentNumber { get; set; }
        [StringLength(80)] public string? DocumentTemplateCode { get; set; }

        [StringLength(200)] public string? EmployerLegalName { get; set; }
        [StringLength(30)] public string? EmployerTaxCode { get; set; }
        [StringLength(500)] public string? EmployerAddress { get; set; }
        [StringLength(150)] public string? EmployerRepresentativeName { get; set; }
        [StringLength(150)] public string? EmployerRepresentativeTitle { get; set; }
        [StringLength(500)] public string? EmployerRepresentativeAuthorization { get; set; }
        [StringLength(200)] public string? SigningLocation { get; set; }

        [StringLength(150)] public string? EmployeeFullNameSnapshot { get; set; }
        public DateTime? EmployeeBirthDateSnapshot { get; set; }
        public Gender? EmployeeGenderSnapshot { get; set; }
        [StringLength(30)] public string? EmployeeIdentityNumberSnapshot { get; set; }
        public DateTime? EmployeeIdentityIssueDate { get; set; }
        [StringLength(150)] public string? EmployeeIdentityIssuePlace { get; set; }
        [StringLength(500)] public string? EmployeeResidenceAddressSnapshot { get; set; }
        [StringLength(150)] public string? EmployeeDepartmentSnapshot { get; set; }
        [StringLength(150)] public string? EmployeePositionSnapshot { get; set; }
        [StringLength(150)] public string? EmployeeJobLevelSnapshot { get; set; }

        [StringLength(150)] public string? JobTitle { get; set; }
        [StringLength(1000)] public string? JobDescription { get; set; }
        [StringLength(500)] public string? WorkLocation { get; set; }
        [StringLength(100)] public string? WorkingMode { get; set; }
        [StringLength(500)] public string? WorkingHours { get; set; }
        [StringLength(500)] public string? RestTime { get; set; }
        [StringLength(150)] public string? DirectManagerSnapshot { get; set; }

        [StringLength(100)] public string? SalaryPaymentMethod { get; set; }
        [StringLength(100)] public string? SalaryPaymentDate { get; set; }
        [StringLength(1000)] public string? AllowanceDescription { get; set; }
        [StringLength(1000)] public string? AdditionalBenefits { get; set; }
        [StringLength(1000)] public string? SalaryReviewPolicy { get; set; }
        [StringLength(1000)] public string? BonusPolicy { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? KpiBonusTargetAmount { get; set; }
        [StringLength(80)] public string? KpiBonusPolicyCode { get; set; }
        [StringLength(80)] public string? KpiBonusPolicyVersionCode { get; set; }
        [Column(TypeName = "text")] public string? KpiScoreFormula { get; set; }
        [Column(TypeName = "text")] public string? KpiPayoutFormula { get; set; }
        [Column(TypeName = "text")] public string? KpiBonusEligibilityRule { get; set; }
        [Column(TypeName = "text")] public string? KpiBonusPaymentPeriod { get; set; }
        [Column(TypeName = "text")] public string? KpiBonusApproverRole { get; set; }
        [StringLength(1000)] public string? InsurancePolicy { get; set; }
        [StringLength(1000)] public string? LaborProtectionPolicy { get; set; }
        [StringLength(1000)] public string? TrainingPolicy { get; set; }

        [Column(TypeName = "text")] public string? EmployeeObligations { get; set; }
        [Column(TypeName = "text")] public string? EmployerObligations { get; set; }
        [Column(TypeName = "text")] public string? ConfidentialityClause { get; set; }
        [Column(TypeName = "text")] public string? IntellectualPropertyClause { get; set; }
        [Column(TypeName = "text")] public string? TerminationClause { get; set; }
        [Column(TypeName = "text")] public string? DisputeResolutionClause { get; set; }

        [StringLength(500)] public string? DocumentDocFilePath { get; set; }
        [StringLength(500)] public string? DocumentPdfFilePath { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public DateTime? EmployerSignedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByAccountId { get; set; }

        [ForeignKey(nameof(CreatedByAccountId))]
        public virtual Account? CreatedByAccount { get; set; }
    }
}
