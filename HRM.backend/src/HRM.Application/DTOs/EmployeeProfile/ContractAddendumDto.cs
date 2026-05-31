namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class CreateContractAddendumDto
    {
        public decimal? NewBasicSalary { get; set; }
        public decimal? NewInsuranceSalary { get; set; }
        public DateTime? NewEndDate { get; set; }
        public string? OtherChangesJson { get; set; }
        public string? Content { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class ReviewContractAddendumDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }

    public class ContractAddendumResponseDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string AddendumNumber { get; set; } = string.Empty;
        public decimal? NewBasicSalary { get; set; }
        public decimal? NewInsuranceSalary { get; set; }
        public DateTime? NewEndDate { get; set; }
        public string? OtherChangesJson { get; set; }
        public string? Content { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public List<ContractAddendumDetailDto> Details { get; set; } = new();
    }

    public class ContractAddendumDetailDto
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
