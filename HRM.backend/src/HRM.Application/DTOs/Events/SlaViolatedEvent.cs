using HRM.backend.src.HRM.Core.Enums;
using MediatR;

namespace HRM.backend.src.HRM.Application.DTOs.Events
{
    public class SlaViolatedEvent : INotification
    {
        public SlaModuleType ModuleType { get; set; }
        public int ReferenceId { get; set; }
    }
}
