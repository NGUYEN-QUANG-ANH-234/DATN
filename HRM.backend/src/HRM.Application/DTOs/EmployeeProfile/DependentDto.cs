using System.ComponentModel.DataAnnotations;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class DependentDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DependentRelation Relationship { get; set; }
        public string? IdNumber { get; set; }
        public string? TaxDependentCode { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; }
        public string? EvidenceUrl { get; set; }
        public string? Note { get; set; }
    }

    public class DependentRequestDto
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        public DependentRelation Relationship { get; set; }
        [StringLength(20)] public string? IdNumber { get; set; }
        [StringLength(20)] public string? TaxDependentCode { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Note { get; set; }
        public IFormFile? EvidenceFile { get; set; }
    }

    public class HrDependentDto
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        public DependentRelation Relationship { get; set; }
        [StringLength(20)] public string? IdNumber { get; set; }
        [StringLength(20)] public string? TaxDependentCode { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }

    public class PendingDependentRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public int? DependentId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string RequestedDataJson { get; set; } = string.Empty;
        public string? EvidenceUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
