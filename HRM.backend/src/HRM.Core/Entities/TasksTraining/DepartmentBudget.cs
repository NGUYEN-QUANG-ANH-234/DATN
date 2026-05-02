using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.Organization; // Khai báo nếu Department ở thư mục khác

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("department_budgets")]
    public class DepartmentBudget
    {
        [Key] public int Id { get; set; }

        public int? DeptId { get; set; }
         [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public DateTime? MonthYear { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? TotalBudget { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? UsedBudget { get; set; } = 0;

        public BudgetStatus Status { get; set; } = BudgetStatus.Pending;
        public DateTime? DeadlineAt { get; set; }

        // Một ngân sách có thể được chia cho nhiều Task
        public virtual ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    }
}