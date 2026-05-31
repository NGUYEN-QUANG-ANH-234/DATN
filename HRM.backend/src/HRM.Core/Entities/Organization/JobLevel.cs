using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.Organization
{
    [Table("job_levels")]
    public class JobLevel
    {
        [Key] public int Id { get; set; }

        [StringLength(30)] public required string Code { get; set; }
        [StringLength(100)] public required string Name { get; set; }

        public int RankOrder { get; set; }
        public bool IsManagementLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<PositionJobLevelPolicy> PositionPolicies { get; set; } = new List<PositionJobLevelPolicy>();
    }
}
