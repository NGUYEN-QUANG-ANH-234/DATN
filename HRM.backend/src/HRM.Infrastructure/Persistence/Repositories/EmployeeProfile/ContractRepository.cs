using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class ContractRepository : BaseRepository<Contract>, IContractRepository
    {
        public ContractRepository(MyDbContext context) : base(context) { }

        public new async Task<Contract?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.Employee)
                .Include(c => c.LegalSnapshots)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task SaveRequestAsync(Contract contract)
        {
            await _dbSet.AddAsync(contract);
        }

        public async Task UpdateStatusAsync(int id, ContractStatus status)
        {
            var contract = await _dbSet.FindAsync(id);
            if (contract != null)
            {
                contract.Status = status;
            }
        }

        public async Task SaveDraftAsync(Contract contract)
        {
            contract.Status = ContractStatus.Draft;
            await _dbSet.AddAsync(contract);
        }

        public async Task SaveNewVersionAsync(Contract contract)
        {
            await _dbSet.AddAsync(contract);
        }

        public async Task<bool> ActivateContractAsync(int id)
        {
            var contract = await _dbSet.FindAsync(id);
            if (contract != null && contract.Status == ContractStatus.ApprovedByDirector)
            {
                contract.Status = ContractStatus.Active;
                return true;
            }
            return false;
        }

        public async Task<List<Contract>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.LegalSnapshots)
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.StartDate)
                .ThenByDescending(c => c.Version)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Contract>> GetByStatusAsync(ContractStatus status, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.Employee)
                .Include(c => c.LegalSnapshots)
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.Id)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Contract>> GetByStatusesAsync(IEnumerable<ContractStatus> statuses, CancellationToken ct = default)
        {
            var statusList = statuses.ToList();
            return await _dbSet
                .Include(c => c.Employee)
                .Include(c => c.LegalSnapshots)
                .Where(c => statusList.Contains(c.Status))
                .OrderByDescending(c => c.Id)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Contract>> GetAllWithEmployeeAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.Employee)
                .Include(c => c.LegalSnapshots)
                .OrderByDescending(c => c.Id)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Contract>> GetContractsWithDetailsAsync(List<int> contractIds, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(c => contractIds.Contains(c.Id))
                .Include(c => c.LegalSnapshots)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}

