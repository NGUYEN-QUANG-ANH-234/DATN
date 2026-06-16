using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PayrollAllowances
{
    public class ExternalTimesheetImportRepository : IExternalTimesheetImportRepository
    {
        private static readonly ExternalTimesheetImportStatus[] DuplicateStatuses =
        {
            ExternalTimesheetImportStatus.Draft,
            ExternalTimesheetImportStatus.Validated,
            ExternalTimesheetImportStatus.Approved
        };

        private static readonly ExternalTimesheetImportStatus[] ReplaceableDuplicateStatuses =
        {
            ExternalTimesheetImportStatus.Draft,
            ExternalTimesheetImportStatus.Validated,
            ExternalTimesheetImportStatus.Rejected
        };

        private readonly MyDbContext _context;

        public ExternalTimesheetImportRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExternalTimesheetImport>> GetBatchesAsync(
            byte? month,
            short? year,
            ExternalTimesheetImportStatus? status,
            CancellationToken ct = default)
        {
            var query = _context.ExternalTimesheetImports
                .Include(i => i.ImportedByAccount)
                .Include(i => i.ApprovedByAccount)
                .AsQueryable();

            if (month.HasValue) query = query.Where(i => i.ImportMonth == month.Value);
            if (year.HasValue) query = query.Where(i => i.ImportYear == year.Value);
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);

            return await query
                .OrderByDescending(i => i.ImportedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<ExternalTimesheetImport?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildDetailQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<ExternalTimesheetImport?> GetTrackedDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildDetailQuery()
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<List<ExternalTimesheetLine>> GetDuplicateCandidatesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            return await _context.ExternalTimesheetLines
                .Include(l => l.Import)
                .Where(l => l.WorkDate.Date >= periodStart.Date &&
                            l.WorkDate.Date <= periodEnd.Date &&
                            DuplicateStatuses.Contains(l.Import.Status))
                .ToListAsync(ct);
        }

        public async Task<List<ExternalTimesheetLine>> GetReplaceableDuplicateCandidatesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            return await _context.ExternalTimesheetLines
                .Include(l => l.Import)
                .Where(l => l.WorkDate.Date >= periodStart.Date &&
                            l.WorkDate.Date <= periodEnd.Date &&
                            ReplaceableDuplicateStatuses.Contains(l.Import.Status))
                .ToListAsync(ct);
        }

        public async Task AddAsync(ExternalTimesheetImport import, CancellationToken ct = default)
        {
            await _context.ExternalTimesheetImports.AddAsync(import, ct);
        }

        public void Update(ExternalTimesheetImport import)
        {
            _context.ExternalTimesheetImports.Update(import);
        }

        public void RemoveLines(IEnumerable<ExternalTimesheetLine> lines)
        {
            _context.ExternalTimesheetLines.RemoveRange(lines);
        }

        private IQueryable<ExternalTimesheetImport> BuildDetailQuery()
        {
            return _context.ExternalTimesheetImports
                .Include(i => i.ImportedByAccount)
                .Include(i => i.ApprovedByAccount)
                .Include(i => i.Lines)
                    .ThenInclude(l => l.CollaboratorEmployee);
        }
    }
}
