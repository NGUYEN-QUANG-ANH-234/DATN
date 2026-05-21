using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("trainings")]
    public class Training
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; } // Nên là ID của Thực tập sinh
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        [StringLength(255)] public string? CourseName { get; set; }

        public TrainingStatus Status { get; set; } = TrainingStatus.InProgress;
        public DateTime? Deadline { get; set; }

        // --- CÁC CỘT CẦN THÊM MỚI ---
        [Column(TypeName = "decimal(5,2)")]
        public decimal? FinalScore { get; set; } // Điểm đánh giá cuối khóa

        [StringLength(1000)]
        public string? ManagerEvaluation { get; set; } // Lời phê của Trưởng phòng

        public bool IsPassed { get; set; } = false; // Đạt/Không đạt để Module 8 bắt sự kiện ký HĐ
    }
}