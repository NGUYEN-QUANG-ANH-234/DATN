namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class PersonnelChangeRiskSummaryDto
    {
        public int RequestId { get; set; }
        public PersonnelChangeEmployeeSnapshotDto? Employee { get; set; }
        public PersonnelChangeContractSnapshotDto? CurrentContract { get; set; }
        public PersonnelChangeContractSnapshotDto? RelatedContract { get; set; }
        public PersonnelChangeContractAddendumSnapshotDto? RelatedAddendum { get; set; }
        public PersonnelChangePerformanceSnapshotDto? LatestPerformance { get; set; }
        public PersonnelChangePenaltySummaryDto PenaltySummary { get; set; } = new();
        public PersonnelChangeSeniorityDto Seniority { get; set; } = new();
        public PersonnelChangePayrollSnapshotDto? LatestPayroll { get; set; }
        public PersonnelChangeAttendanceSnapshotDto? LatestAttendance { get; set; }
        public List<PersonnelChangeHistorySummaryItemDto> History { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class PersonnelChangeEmployeeSnapshotDto
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public string? JobLevelName { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public DateTime? JoinedDate { get; set; }
    }

    public class PersonnelChangeContractSnapshotDto
    {
        public int Id { get; set; }
        public string? ContractNumber { get; set; }
        public string? ContractType { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal InsuranceSalary { get; set; }
    }

    public class PersonnelChangeContractAddendumSnapshotDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string? AddendumNumber { get; set; }
        public string? Status { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal? NewBasicSalary { get; set; }
        public decimal? NewInsuranceSalary { get; set; }
    }

    public class PersonnelChangePerformanceSnapshotDto
    {
        public int Id { get; set; }
        public string Period { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? FinalRating { get; set; }
        public string? Status { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public List<PersonnelChangeKpiSnapshotDto> Kpis { get; set; } = new();
    }

    public class PersonnelChangeKpiSnapshotDto
    {
        public int Id { get; set; }
        public string KpiCode { get; set; } = string.Empty;
        public string KpiName { get; set; } = string.Empty;
        public int WeightPercent { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? ActualValue { get; set; }
        public string? Unit { get; set; }
        public decimal AchievedPercent { get; set; }
        public decimal ManagerScore { get; set; }
        public decimal FinalPoint { get; set; }
        public decimal PenaltyPoint { get; set; }
        public string? PenaltyReason { get; set; }
    }

    public class PersonnelChangePenaltySummaryDto
    {
        public int TotalRecords { get; set; }
        public int PersonnelImpactRecords { get; set; }
        public decimal TotalPenaltyPoint { get; set; }
        public List<PersonnelChangePenaltyItemDto> LatestRecords { get; set; } = new();
    }

    public class PersonnelChangePenaltyItemDto
    {
        public int Id { get; set; }
        public string Period { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string RuleCode { get; set; } = string.Empty;
        public decimal PenaltyPoint { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime? OccurredAt { get; set; }
    }

    public class PersonnelChangeSeniorityDto
    {
        public DateTime? JoinedDate { get; set; }
        public int TotalMonths { get; set; }
        public decimal TotalYears { get; set; }
    }

    public class PersonnelChangePayrollSnapshotDto
    {
        public int Id { get; set; }
        public byte? Month { get; set; }
        public short? Year { get; set; }
        public string? Period { get; set; }
        public string? Status { get; set; }
        public decimal? GrossIncome { get; set; }
        public decimal? NetSalary { get; set; }
        public DateTime? CalculatedAt { get; set; }
    }

    public class PersonnelChangeAttendanceSnapshotDto
    {
        public int Id { get; set; }
        public byte Month { get; set; }
        public short Year { get; set; }
        public decimal WorkDays { get; set; }
        public int WorkedMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int ActualOtMinutes { get; set; }
        public bool IsPayrollLocked { get; set; }
    }

    public class PersonnelChangeHistorySummaryItemDto
    {
        public DateTime Date { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? RefId { get; set; }
    }
}
