using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class CreateSeniorAppointmentDto
    {
        public int EmployeeId { get; set; }
        public int? NewDepartmentId { get; set; }
        public int NewPositionId { get; set; }
        public int? NewJobLevelId { get; set; }
        public int? ReportsToManagerId { get; set; }
        public bool IsDepartmentManager { get; set; }
        public string? Reason { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public int? RelatedContractId { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.ContractAddendum;
    }

    public class AppointmentConsentDto
    {
        public bool IsAccepted { get; set; }
        public string? Note { get; set; }
    }

    public class HrContractFlowDto
    {
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.ContractAddendum;
        public int? RelatedContractId { get; set; }
        public string? Note { get; set; }
    }

    public class IssueAppointmentDecisionDto
    {
        public string DecisionNumber { get; set; } = string.Empty;
        public string? DecisionFilePath { get; set; }
        public DateTime? DecisionIssuedAt { get; set; }
        public string? Note { get; set; }
    }
}
