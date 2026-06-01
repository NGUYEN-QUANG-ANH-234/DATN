namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class SubmitResignationDto
    {
        public int EmployeeId { get; set; }
        public DateTime ExpectedLastWorkingDate { get; set; }
        public string? Reason { get; set; }
        public string? EmployeeNote { get; set; }
    }

    public class ManagerReviewResignationDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class HrReviewResignationDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
        public int? RelatedContractId { get; set; }
        public bool RequiresFinalSettlement { get; set; } = true;
        public bool LockAccountAfterEffectiveDate { get; set; } = true;
    }

    public class DirectorApproveResignationDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }
}
