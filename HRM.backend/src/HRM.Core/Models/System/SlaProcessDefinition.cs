namespace HRM.backend.src.HRM.Core.Models.System
{
    public class SlaProcessDefinition
    {
        public required string Code { get; set; }
        public required string DisplayName { get; set; }
        public required string ModuleName { get; set; }
        public required string Description { get; set; }
        public int DefaultValue { get; set; }
        public string DefaultUnit { get; set; } = "HOURS";
    }

    public class SlaProcessAlias
    {
        public required string LegacyCode { get; set; }
        public required string CanonicalCode { get; set; }
    }
}
