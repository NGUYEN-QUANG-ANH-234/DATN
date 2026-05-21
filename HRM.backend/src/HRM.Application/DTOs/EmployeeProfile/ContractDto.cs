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
    }

    public class NegotiateDto
    {
        public string NegotiationNote { get; set; } = null!;
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
    }
}
