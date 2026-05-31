using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("external_timesheet_lines")]
    public class ExternalTimesheetLine
    {
        [Key] public int Id { get; set; }

        public int ImportId { get; set; }
        [ForeignKey("ImportId")] public virtual ExternalTimesheetImport Import { get; set; } = null!;

        public int? CollaboratorEmployeeId { get; set; }
        [ForeignKey("CollaboratorEmployeeId")] public virtual Employee? CollaboratorEmployee { get; set; }

        [StringLength(50)] public string? CollaboratorCode { get; set; }

        public DateTime WorkDate { get; set; }
        [StringLength(80)] public string? ProjectCode { get; set; }
        [StringLength(80)] public string? TaskCode { get; set; }

        [Column(TypeName = "decimal(7,2)")] public decimal ApprovedHours { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal HourlyRate { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }

        public bool IsPayrollImported { get; set; }
        public int? PayrollId { get; set; }
        [ForeignKey("PayrollId")] public virtual Payroll? Payroll { get; set; }

        [StringLength(1000)] public string? Note { get; set; }
    }
}
