using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class ContractAddendumRepository : BaseRepository<ContractAddendum>, IContractAddendumRepository
    {
        public ContractAddendumRepository(MyDbContext context) : base(context) { }

        public async Task<ContractAddendum?> GetByIdWithContractAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Details)
                .Include(a => a.Contract)
                    .ThenInclude(c => c!.Employee)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<List<ContractAddendum>> GetByContractIdAsync(int contractId, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Details)
                .Include(a => a.Contract)
                    .ThenInclude(c => c!.Employee)
                .Where(a => a.ContractId == contractId)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ContractAddendum>> GetByStatusAsync(AddendumStatus status, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Details)
                .Include(a => a.Contract)
                    .ThenInclude(c => c!.Employee)
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ContractAddendum>> GetAllWithContractAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Details)
                .Include(a => a.Contract)
                    .ThenInclude(c => c!.Employee)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
