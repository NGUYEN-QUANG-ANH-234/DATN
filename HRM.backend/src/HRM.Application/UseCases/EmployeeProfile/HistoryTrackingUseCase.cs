using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Models.History;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class HistoryTrackingUseCase : IHistoryTrackingUseCase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IHistoryTrackingRepository _historyRepo;

        public HistoryTrackingUseCase(
            IEmployeeRepository employeeRepo,
            IHistoryTrackingRepository historyRepo)
        {
            _employeeRepo = employeeRepo;
            _historyRepo = historyRepo;
        }

        public async Task<PaginatedHistoryResponse> GetConsolidatedHistoryAsync(
            int accountId,
            HistoryFilterDto filter,
            CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null)
                throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân sự.");

            var criteria = new HistoryFilterCriteria
            {
                Year = filter.Year,
                Type = string.IsNullOrWhiteSpace(filter.Type) ? "ALL" : filter.Type.Trim().ToUpperInvariant(),
                Page = filter.Page <= 0 ? 1 : filter.Page,
                Size = filter.Size <= 0 ? 10 : Math.Min(filter.Size, 50)
            };

            var page = await _historyRepo.GetPagedConsolidatedHistoryAsync(employee.Id, criteria, ct);

            return new PaginatedHistoryResponse
            {
                Items = page.Items.Select(item => new ConsolidatedHistoryResponse
                {
                    Date = item.Date,
                    EventType = item.EventType,
                    Title = item.Title,
                    Description = item.Description,
                    RefId = item.RefId,
                    OldValue = item.OldValue,
                    NewValue = item.NewValue
                }).ToList(),
                TotalCount = page.TotalCount,
                Page = page.Page,
                Size = page.Size
            };
        }
    }
}
