namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class SourceCatalogDto
    {
        public int Id { get; set; }
        public required string DisplayName { get; set; }
        public required string SourcePath { get; set; }
        public required string Module { get; set; }
        public required string DataType { get; set; }
        public required string AggregationType { get; set; }
        public bool IsPeriodBased { get; set; }
        public bool IsActive { get; set; }
    }

    public class SourceCatalogStatusDto
    {
        public bool IsActive { get; set; }
    }
}
