using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IContractAddendumRepository : IBaseRepository<ContractAddendum>
    {
        Task<ContractAddendum?> GetByIdWithContractAsync(int id, CancellationToken ct = default);
        Task<List<ContractAddendum>> GetByContractIdAsync(int contractId, CancellationToken ct = default);
        Task<List<ContractAddendum>> GetByStatusAsync(AddendumStatus status, CancellationToken ct = default);
        Task<List<ContractAddendum>> GetAllWithContractAsync(CancellationToken ct = default);
    }
}
