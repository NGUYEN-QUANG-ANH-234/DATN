using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class ContractRepository : BaseRepository<Contract>, IContractRepository
    {
        public ContractRepository(MyDbContext context) : base(context) { }

        public async Task SaveRequestAsync(Contract contract)
        {
            await _dbSet.AddAsync(contract);
        }

        public async Task UpdateStatusAsync(int id, ContractStatus status)
        {
            var contract = await _dbSet.FindAsync(id);
            if (contract != null)
            {
                contract.Status = status; // Trạng thái: Draft, Active, Expired...
            }
        }

        public async Task SaveDraftAsync(Contract contract)
        {
            contract.Status = ContractStatus.Draft;
            await _dbSet.AddAsync(contract);
        }

        public async Task SaveNewVersionAsync(Contract contract)
        {
            // Nghiệp vụ ký lại: Insert version mới
            await _dbSet.AddAsync(contract);
        }

        public async Task<bool> ActivateContractAsync(int id)
        {
            var contract = await _dbSet.FindAsync(id);
            if (contract != null && contract.Status == ContractStatus.Draft)
            {
                contract.Status = ContractStatus.Active;
                return true;
            }
            return false;
        }
    }
}
