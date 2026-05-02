using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance
{
    public class LeaveRequestRepository : BaseRepository<LeaveRequest>, ILeaveRequestRepository
    {
        public LeaveRequestRepository(MyDbContext context) : base(context) { }

        public async Task AddRequestAsync(LeaveRequest request) => await _dbSet.AddAsync(request);

        public async Task UpdateRequestStatusAsync(int id, LeaveRequestStatus status, DateTime? deadline = null)
        {
            var request = await _dbSet.FindAsync(id);
            if (request != null)
            {
                request.Status = status;
                if (deadline.HasValue) request.DeadlineAt = deadline.Value;
            }
        }

        public async Task<IEnumerable<LeaveRequest>> FetchExpiredRequestsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == LeaveRequestStatus.Pending && r.DeadlineAt < DateTime.UtcNow)
                .ToListAsync();
        }
    }
}
