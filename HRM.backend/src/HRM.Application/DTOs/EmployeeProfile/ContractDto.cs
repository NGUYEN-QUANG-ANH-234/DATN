namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class ContractRequestDto
    {
        public string? Note { get; set; }
    }

    public class ReviewContractDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }

    public class CreateDraftDto
    {
        public string ContractType { get; set; } = null!;
        public decimal BasicSalary { get; set; }
        public decimal SalaryPercentage { get; set; }
        public decimal InsuranceSalary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ContractEmployerDraftDto? Employer { get; set; }
        public ContractEmployeeDraftDto? Employee { get; set; }
        public ContractWorkDraftDto? Work { get; set; }
        public ContractCompensationDraftDto? Compensation { get; set; }
        public ContractClauseDraftDto? Clauses { get; set; }
        public ContractIssuanceDraftDto? Issuance { get; set; }
    }

    public class ContractEmployerDraftDto
    {
        public string? LegalName { get; set; }
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? RepresentativeName { get; set; }
        public string? RepresentativeTitle { get; set; }
        public string? RepresentativeAuthorization { get; set; }
        public string? SigningLocation { get; set; }
    }

    public class ContractEmployeeDraftDto
    {
        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? IdentityNumber { get; set; }
        public DateTime? IdentityIssueDate { get; set; }
        public string? IdentityIssuePlace { get; set; }
        public string? ResidenceAddress { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? JobLevel { get; set; }
    }

    public class ContractWorkDraftDto
    {
        public string? JobTitle { get; set; }
        public string? JobDescription { get; set; }
        public string? WorkLocation { get; set; }
        public string? WorkingMode { get; set; }
        public string? WorkingHours { get; set; }
        public string? RestTime { get; set; }
        public string? DirectManager { get; set; }
    }

    public class ContractCompensationDraftDto
    {
        public string? SalaryPaymentMethod { get; set; }
        public string? SalaryPaymentDate { get; set; }
        public string? AllowanceDescription { get; set; }
        public string? AdditionalBenefits { get; set; }
        public string? SalaryReviewPolicy { get; set; }
        public string? BonusPolicy { get; set; }
        public string? InsurancePolicy { get; set; }
        public string? LaborProtectionPolicy { get; set; }
        public string? TrainingPolicy { get; set; }
    }

    public class ContractClauseDraftDto
    {
        public string? EmployeeObligations { get; set; }
        public string? EmployerObligations { get; set; }
        public string? ConfidentialityClause { get; set; }
        public string? IntellectualPropertyClause { get; set; }
        public string? TerminationClause { get; set; }
        public string? DisputeResolutionClause { get; set; }
    }

    public class ContractIssuanceDraftDto
    {
        public string? LegalDocumentNumber { get; set; }
        public string? DocumentTemplateCode { get; set; }
        public DateTime? IssuedAt { get; set; }
    }

    public class NegotiateDto
    {
        public string NegotiationNote { get; set; } = null!;
    }

    public class RequestRevisionDto
    {
        public string Reason { get; set; } = null!;
    }

    // DTO trả về danh sách hợp đồng
    public class ContractResponseDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = null!;
        public string? ContractType { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal SalaryPercentage { get; set; }
        public decimal InsuranceSalary { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public int Version { get; set; }
        public string? NegotiationNote { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? LegalDocumentType { get; set; }
        public string? EmployerLegalName { get; set; }
        public string? EmployerTaxCode { get; set; }
        public string? EmployerAddress { get; set; }
        public string? EmployerRepresentativeName { get; set; }
        public string? EmployerRepresentativeTitle { get; set; }
        public string? EmployerRepresentativeAuthorization { get; set; }
        public string? SigningLocation { get; set; }
        public string? EmployeeFullNameSnapshot { get; set; }
        public DateTime? EmployeeBirthDateSnapshot { get; set; }
        public string? EmployeeGenderSnapshot { get; set; }
        public string? EmployeeIdentityNumberSnapshot { get; set; }
        public DateTime? EmployeeIdentityIssueDate { get; set; }
        public string? EmployeeIdentityIssuePlace { get; set; }
        public string? EmployeeResidenceAddressSnapshot { get; set; }
        public string? EmployeeDepartmentSnapshot { get; set; }
        public string? EmployeePositionSnapshot { get; set; }
        public string? EmployeeJobLevelSnapshot { get; set; }
        public string? JobTitle { get; set; }
        public string? JobDescription { get; set; }
        public string? WorkLocation { get; set; }
        public string? WorkingMode { get; set; }
        public string? WorkingHours { get; set; }
        public string? RestTime { get; set; }
        public string? DirectManagerSnapshot { get; set; }
        public string? SalaryPaymentMethod { get; set; }
        public string? SalaryPaymentDate { get; set; }
        public string? AllowanceDescription { get; set; }
        public string? AdditionalBenefits { get; set; }
        public string? SalaryReviewPolicy { get; set; }
        public string? BonusPolicy { get; set; }
        public decimal? KpiBonusTargetAmount { get; set; }
        public string? KpiBonusPolicyCode { get; set; }
        public string? KpiBonusPolicyVersionCode { get; set; }
        public string? KpiScoreFormula { get; set; }
        public string? KpiPayoutFormula { get; set; }
        public string? KpiBonusEligibilityRule { get; set; }
        public string? KpiBonusPaymentPeriod { get; set; }
        public string? KpiBonusApproverRole { get; set; }
        public string? InsurancePolicy { get; set; }
        public string? LaborProtectionPolicy { get; set; }
        public string? TrainingPolicy { get; set; }
        public string? EmployeeObligations { get; set; }
        public string? EmployerObligations { get; set; }
        public string? ConfidentialityClause { get; set; }
        public string? IntellectualPropertyClause { get; set; }
        public string? TerminationClause { get; set; }
        public string? DisputeResolutionClause { get; set; }
        public string? LegalDocumentNumber { get; set; }
        public string? DocumentTemplateCode { get; set; }
        public string? DocumentDocFilePath { get; set; }
        public string? DocumentPdfFilePath { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public DateTime? EmployerSignedAt { get; set; }
    }
}
