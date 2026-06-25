using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("insurance_configs")]
    public class InsuranceConfig
    {
        [Key] public int Id { get; set; }

        [StringLength(80)] public required string Code { get; set; }
        [StringLength(200)] public required string Name { get; set; }

        [Column(TypeName = "decimal(7,4)")] public decimal SocialInsuranceEmployeeRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal HealthInsuranceEmployeeRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal UnemploymentInsuranceEmployeeRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal SocialInsuranceEmployerRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal HealthInsuranceEmployerRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal UnemploymentInsuranceEmployerRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal UnionFeeEmployerRate { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? MinInsuranceSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? MaxInsuranceSalary { get; set; }
        public int UnpaidLeaveNoContributionThresholdDays { get; set; } = 14;
        public int MinContractMonthsForContribution { get; set; } = 1;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        [StringLength(80)] public string? VersionCode { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;
        [StringLength(200)] public string? SourceRef { get; set; }
        public int? SupersedesVersionId { get; set; }
        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool LockedAfterUsed { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
