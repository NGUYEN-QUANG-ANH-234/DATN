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
}
