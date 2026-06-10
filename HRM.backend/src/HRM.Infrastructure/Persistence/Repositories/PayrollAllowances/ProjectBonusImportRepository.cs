using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PayrollAllowances
{
    public class ProjectBonusImportRepository : IProjectBonusImportRepository
    {
        private static readonly ProjectBonusImportStatus[] DuplicateStatuses =
        {
            ProjectBonusImportStatus.Draft,
            ProjectBonusImportStatus.PendingReview,
            ProjectBonusImportStatus.Approved
        };

        private static readonly ProjectBonusImportStatus[] ReplaceableDuplicateStatuses =
        {
            ProjectBonusImportStatus.Draft,
            ProjectBonusImportStatus.PendingReview
        };

        private readonly MyDbContext _context;

        public ProjectBonusImportRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectBonusImportBatch>> GetBatchesAsync(
            byte? month,
            short? year,
            ProjectBonusImportStatus? status,
            CancellationToken ct = default)
        {
            var query = _context.ProjectBonusImportBatches
                .Include(b => b.UploadedByAccount)
                .Include(b => b.ApprovedByAccount)
                .AsQueryable();

            if (month.HasValue) query = query.Where(b => b.PeriodMonth == month.Value);
            if (year.HasValue) query = query.Where(b => b.PeriodYear == year.Value);
            if (status.HasValue) query = query.Where(b => b.Status == status.Value);

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<ProjectBonusImportBatch?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildDetailQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<ProjectBonusImportBatch?> GetTrackedDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildDetailQuery()
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetDuplicateCandidatesAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Where(l => l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            DuplicateStatuses.Contains(l.Batch.Status))
                .ToListAsync(ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetReplaceableDuplicateCandidatesAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Where(l => l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            ReplaceableDuplicateStatuses.Contains(l.Batch.Status))
                .ToListAsync(ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetApprovedLinesAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Where(l => l.EmployeeId.HasValue &&
                            l.ValidationStatus == ProjectBonusLineValidationStatus.Valid &&
                            l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            l.Batch.Status == ProjectBonusImportStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetApprovedLinesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<ProjectBonusImportLine>();

            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Where(l => l.EmployeeId.HasValue &&
                            ids.Contains(l.EmployeeId.Value) &&
                            l.ValidationStatus == ProjectBonusLineValidationStatus.Valid &&
                            l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            l.Batch.Status == ProjectBonusImportStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddAsync(ProjectBonusImportBatch batch, CancellationToken ct = default)
        {
            await _context.ProjectBonusImportBatches.AddAsync(batch, ct);
        }

        public void Update(ProjectBonusImportBatch batch)
        {
            _context.ProjectBonusImportBatches.Update(batch);
        }

        public void RemoveLines(IEnumerable<ProjectBonusImportLine> lines)
        {
            _context.ProjectBonusImportLines.RemoveRange(lines);
        }

        private IQueryable<ProjectBonusImportBatch> BuildDetailQuery()
        {
            return _context.ProjectBonusImportBatches
                .Include(b => b.UploadedByAccount)
                .Include(b => b.ApprovedByAccount)
                .Include(b => b.Lines)
                    .ThenInclude(l => l.Employee);
        }
    }
}
