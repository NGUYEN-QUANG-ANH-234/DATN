using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class PersonnelChangeTimelineDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public PersonnelChangeStatus? OldStatus { get; set; }
        public PersonnelChangeStatus? NewStatus { get; set; }
        public int? ActorAccountId { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
