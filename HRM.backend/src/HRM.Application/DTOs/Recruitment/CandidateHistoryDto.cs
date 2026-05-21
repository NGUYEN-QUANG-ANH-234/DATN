using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.Recruitment
{
    public class CandidateHistoryDto
    {
        public int CandidateId { get; set; }
        public int RecruitmentRequestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CvFilePath { get; set; }
        public string Email { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
    }
}
