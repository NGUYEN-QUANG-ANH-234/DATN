using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IContractRepository : IBaseRepository<Contract>
    {
        Task SaveRequestAsync(Contract contract);
        Task UpdateStatusAsync(int id, ContractStatus status); // Dùng chung cho cả Status và DraftStatus
        Task SaveDraftAsync(Contract contract);
        Task SaveNewVersionAsync(Contract contract);
        Task<bool> ActivateContractAsync(int id);
    }
}
