using MediatR;

namespace HRM.backend.src.HRM.Application.DTOs.Events
{
    public class OnboardingCompletedEvent : INotification
    {
        public int EmployeeId { get; set; }
        public string EmpCode { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }
}
