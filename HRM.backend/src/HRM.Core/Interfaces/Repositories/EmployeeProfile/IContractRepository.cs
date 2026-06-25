using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IContractRepository : IBaseRepository<Contract>
    {
        Task SaveRequestAsync(Contract contract);
        Task UpdateStatusAsync(int id, ContractStatus status);
        Task SaveDraftAsync(Contract contract);
        Task SaveNewVersionAsync(Contract contract);
        Task<bool> ActivateContractAsync(int id);
        Task<List<Contract>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
        // Query by status (for Manager, HR, Director views)
        Task<List<Contract>> GetByStatusAsync(ContractStatus status, CancellationToken ct = default);
        Task<List<Contract>> GetByStatusesAsync(IEnumerable<ContractStatus> statuses, CancellationToken ct = default);
        Task<List<Contract>> GetByStatusesForDepartmentManagerAsync(IEnumerable<ContractStatus> statuses, int managerAccountId, CancellationToken ct = default);
        // Get all with employee info (for HR/Admin)
        Task<List<Contract>> GetAllWithEmployeeAsync(CancellationToken ct = default);
        Task<List<Contract>> GetContractsWithDetailsAsync(List<int> contractIds, CancellationToken ct = default);
    }
}

