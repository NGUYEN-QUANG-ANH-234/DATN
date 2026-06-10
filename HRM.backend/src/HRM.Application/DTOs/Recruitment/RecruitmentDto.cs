using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.Recruitment
{
    public class CreateRecruitmentDto
    {
        public int? DeptId { get; set; }
        public int? PositionId { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }

    }

    // Dành cho HR / Giám đốc duyệt
    public class ReviewRecruitmentDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class CloseRecruitmentRequestDto
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class RecruitmentRequestListItemDto
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int FilledSlots { get; set; }
        public int ActiveCandidateCount { get; set; }
        public int RemainingSlots { get; set; }
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public bool IsClosed { get; set; }
        public bool IsExpired { get; set; }
        public bool IsFull { get; set; }
        public bool CanApply { get; set; }
    }
}
