using MediatR;

namespace HRM.backend.src.HRM.Application.DTOs.Events
{
    public class ContractActivatedEvent : INotification
    {
        public int ContractId { get; set; }
        public int EmployeeId { get; set; }
        public decimal BasicSalary { get; set; }
        public DateTime StartDate { get; set; }
    }    
}
