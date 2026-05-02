using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class ShiftRepository : BaseRepository<WorkShift>, IShiftRepository
    {
        public ShiftRepository(MyDbContext context) : base(context) { }

        public async Task AddOrUpdateShiftAsync(WorkShift shift)
        {
            var existing = await _dbSet.FirstOrDefaultAsync(s => s.Id == shift.Id);
            if (existing == null)
            {
                await _dbSet.AddAsync(shift);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(shift);
            }
        }

        public async Task<IEnumerable<WorkShift>> FetchShiftDetailsAsync() => await _dbSet.ToListAsync();
    }
}
