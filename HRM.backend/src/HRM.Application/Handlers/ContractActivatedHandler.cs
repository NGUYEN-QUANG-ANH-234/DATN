using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class ContractActivatedHandler : INotificationHandler<ContractActivatedEvent>
    {
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;

        public ContractActivatedHandler(IBaseRepository<EmploymentHistory> historyRepo)
        {
            _historyRepo = historyRepo;
        }

        public async Task Handle(ContractActivatedEvent notification, CancellationToken ct)
        {
            // Ghi nhận biến động lương / chức vụ vào EmploymentHistory
            var history = new EmploymentHistory
            {
                EmployeeId = notification.EmployeeId,
                Type = HistoryType.Salary_Change,
                EffectiveDate = notification.StartDate,
                // Thay vì dùng Note, ta sử dụng NewValue để lưu thông tin chi tiết
                NewValue = $"Kích hoạt mức lương mới: {notification.BasicSalary:N0} theo hợp đồng ID: {notification.ContractId}",
                // ChangeDate đã có giá trị mặc định là DateTime.UtcNow trong entity, 
                // nhưng nếu muốn set thủ công để tường minh:
                ChangeDate = DateTime.UtcNow
            };

            await _historyRepo.AddAsync(history, ct);
        }
    }
}
