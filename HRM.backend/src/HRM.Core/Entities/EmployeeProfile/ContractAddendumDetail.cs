using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("contract_addendum_details")]
    public class ContractAddendumDetail
    {
        [Key] public int Id { get; set; }

        public int ContractAddendumId { get; set; }
        [ForeignKey("ContractAddendumId")] public virtual ContractAddendum ContractAddendum { get; set; } = null!;

        [StringLength(120)] public required string FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public ContractAddendumDetailValueType ValueType { get; set; } = ContractAddendumDetailValueType.Text;

        [StringLength(500)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
