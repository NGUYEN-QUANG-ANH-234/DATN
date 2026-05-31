using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("overtime_segments")]
    public class OvertimeSegment
    {
        [Key] public int Id { get; set; }

        public int OvertimeRequestId { get; set; }
        [ForeignKey("OvertimeRequestId")] public virtual OvertimeRequest OvertimeRequest { get; set; } = null!;

        public DateTime SegmentStartAt { get; set; }
        public DateTime SegmentEndAt { get; set; }
        public int Minutes { get; set; }
        public OvertimeType OvertimeType { get; set; } = OvertimeType.Weekday;

        [StringLength(80)]
        public required string PolicyCode { get; set; }

        public decimal RateMultiplierSnapshot { get; set; }
        public decimal TaxableAmountSnapshot { get; set; }
        public decimal TaxExemptAmountSnapshot { get; set; }
        public string? PolicySnapshotJson { get; set; }
    }
}
