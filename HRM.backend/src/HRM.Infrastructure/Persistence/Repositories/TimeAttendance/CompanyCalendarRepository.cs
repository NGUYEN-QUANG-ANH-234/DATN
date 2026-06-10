using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class CompanyCalendarRepository : BaseRepository<CompanyCalendar>, ICompanyCalendarRepository
    {
        public CompanyCalendarRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<CompanyCalendar?> GetByIdWithDaysAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.Days)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<List<CompanyCalendar>> GetByYearAsync(short year, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(c => c.Days)
                .AsNoTracking()
                .Where(c => c.Year == year)
                .OrderByDescending(c => c.Status == PolicyVersionStatus.Active)
                .ThenByDescending(c => c.EffectiveFrom)
                .ToListAsync(ct);
        }

        public async Task<CompanyCalendar?> GetActiveByYearAsync(short year, CancellationToken ct = default)
        {
            var yearEnd = new DateTime(year, 12, 31);
            return await _dbSet
                .Include(c => c.Days)
                .AsNoTracking()
                .Where(c =>
                    c.Year == year &&
                    c.Status == PolicyVersionStatus.Active &&
                    c.EffectiveFrom.Date <= yearEnd &&
                    (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= new DateTime(year, 1, 1)))
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<CompanyCalendar?> GetActiveByDateAsync(DateTime date, CancellationToken ct = default)
        {
            var target = date.Date;
            return await _dbSet
                .Include(c => c.Days)
                .AsNoTracking()
                .Where(c =>
                    c.Year == target.Year &&
                    c.Status == PolicyVersionStatus.Active &&
                    c.EffectiveFrom.Date <= target &&
                    (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= target))
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefaultAsync(ct);
        }
    }
}
