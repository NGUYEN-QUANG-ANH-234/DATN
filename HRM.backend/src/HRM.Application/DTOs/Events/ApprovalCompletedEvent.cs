using HRM.backend.src.HRM.Core.Enums;
using MediatR;

namespace HRM.backend.src.HRM.Application.DTOs.Events
{
    public class ApprovalCompletedEvent : INotification
    {
        public required string ModuleCode { get; set; }
        public int ReferenceId { get; set; }
        public ApprovalStatus FinalStatus { get; set; }
        public ApprovalWorkflowAction Action { get; set; }
        public string? Note { get; set; }
    }

    public class ApprovalLevelChangedEvent : INotification
    {
        public required string ModuleCode { get; set; }
        public int ReferenceId { get; set; }
        public int NewLevel { get; set; }
    }
}
