using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PersonnelChanges
{
    public class PersonnelChangeRepository : BaseRepository<PersonnelChangeRequest>, IPersonnelChangeRepository
    {
        public PersonnelChangeRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<PersonnelChangeRequest?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Employee)
                .Include(r => r.CurrentDepartment)
                .Include(r => r.NewDepartment)
                .Include(r => r.CurrentPosition)
                .Include(r => r.NewPosition)
                .Include(r => r.CurrentJobLevel)
                .Include(r => r.NewJobLevel)
                .Include(r => r.CurrentManager)
                .Include(r => r.NewManager)
                .Include(r => r.RequestedByAccount)
                .Include(r => r.DirectorApprovedByAccount)
                .Include(r => r.HRAssignedAccount)
                .Include(r => r.RelatedContract)
                .Include(r => r.RelatedContractAddendum)
                .Include(r => r.SourcePenaltyRecord)
                .Include(r => r.SourcePerformanceReview)
                .Include(r => r.Approvals)
                .Include(r => r.Histories)
                .Include(r => r.ContractLinks)
                .Include(r => r.RiskSnapshots)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<List<PersonnelChangeRequest>> GetByFilterAsync(
            PersonnelChangeType? changeType,
            PersonnelChangeStatus? status,
            int? employeeId,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .Include(r => r.Employee)
                .Include(r => r.CurrentDepartment)
                .Include(r => r.NewDepartment)
                .Include(r => r.CurrentPosition)
                .Include(r => r.NewPosition)
                .AsNoTracking()
                .AsQueryable();

            if (changeType.HasValue)
                query = query.Where(r => r.ChangeType == changeType.Value);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (employeeId.HasValue)
                query = query.Where(r => r.EmployeeId == employeeId.Value);

            if (requestedFrom.HasValue)
                query = query.Where(r => r.RequestedAt >= requestedFrom.Value);

            if (requestedTo.HasValue)
                query = query.Where(r => r.RequestedAt <= requestedTo.Value);

            return await query
                .OrderByDescending(r => r.RequestedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync(ct);
        }

        public async Task<List<PersonnelChangeRequest>> GetByContractFlowReferenceAsync(
            int? contractId,
            int? contractAddendumId,
            CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.ContractLinks)
                .Include(r => r.Histories)
                .Where(r => r.RequiresContractFlow &&
                            ((contractId.HasValue &&
                              (r.RelatedContractId == contractId.Value ||
                               r.RelatedContractRequestId == contractId.Value ||
                               r.ContractLinks.Any(l => l.ContractId == contractId.Value ||
                                                        l.ContractRequestId == contractId.Value))) ||
                             (contractAddendumId.HasValue &&
                              (r.RelatedContractAddendumId == contractAddendumId.Value ||
                               r.ContractLinks.Any(l => l.ContractAddendumId == contractAddendumId.Value)))))
                .ToListAsync(ct);
        }

        public async Task<List<PersonnelChangeHistory>> GetTimelineAsync(int requestId, CancellationToken ct = default)
        {
            return await _context.Set<PersonnelChangeHistory>()
                .Where(h => h.RequestId == requestId)
                .Include(h => h.ActorAccount)
                .OrderBy(h => h.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddHistoryAsync(PersonnelChangeHistory history, CancellationToken ct = default)
        {
            await _context.Set<PersonnelChangeHistory>().AddAsync(history, ct);
        }

        public async Task AddApprovalAsync(PersonnelChangeApproval approval, CancellationToken ct = default)
        {
            await _context.Set<PersonnelChangeApproval>().AddAsync(approval, ct);
        }

        public async Task AddContractLinkAsync(PersonnelChangeContractLink link, CancellationToken ct = default)
        {
            await _context.Set<PersonnelChangeContractLink>().AddAsync(link, ct);
        }

        public async Task AddRiskSnapshotAsync(PersonnelChangeRiskSnapshot snapshot, CancellationToken ct = default)
        {
            await _context.Set<PersonnelChangeRiskSnapshot>().AddAsync(snapshot, ct);
        }
    }
}
