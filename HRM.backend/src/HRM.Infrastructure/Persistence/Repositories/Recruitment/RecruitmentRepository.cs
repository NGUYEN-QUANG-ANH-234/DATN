using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Recruitment
{
    public class RecruitmentRequestRepository : BaseRepository<RecruitmentRequest>, IRecruitmentRequestRepository
    {
        public RecruitmentRequestRepository(MyDbContext context) : base(context) { }

        public async Task<List<RecruitmentRequest>> GetActiveJobPostingsAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Include(r => r.Candidates)
                .Where(r => r.Status == RecruitmentRequestStatus.Approved &&
                           (r.Deadline == null || r.Deadline.Value.Date >= DateTime.UtcNow.Date) &&
                           r.Candidates.Count(c => c.Status == CandidateStatus.Offer || c.Status == CandidateStatus.Hired) < r.Quantity)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<RecruitmentRequest>> GetRequestsByStatusAsync(RecruitmentRequestStatus status, CancellationToken ct = default)
        {
            return await _context.RecruitmentRequests
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Where(r => r.Status == status)
                .AsNoTracking() // Tối ưu hiệu năng đọc dữ liệu
                .ToListAsync(ct);
        }

        public async Task<List<RecruitmentRequest>> GetRequestsByCreatorAsync(int userId, CancellationToken ct = default)
        {
            return await _context.RecruitmentRequests
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Where(r => r.CreatedById == userId)
                .OrderByDescending(r => r.CreatedAt) // Sắp xếp đơn mới nhất lên đầu
                .AsNoTracking() // Tối ưu hiệu năng đọc
                .ToListAsync(ct);
        }

        public async Task<List<RecruitmentRequest>> GetRequestsWithDetailsAsync(List<int> ids, CancellationToken ct = default)
        {
            return await _context.RecruitmentRequests
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Where(r => ids.Contains(r.Id))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<RecruitmentRequest>> GetRequestsWithCandidatesAsync(CancellationToken ct = default)
        {
            return await _context.RecruitmentRequests
                .Include(r => r.Department)
                .Include(r => r.Position)
                .Include(r => r.Candidates)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<RecruitmentRequest?> GetByIdWithCandidatesAsync(int id, CancellationToken ct = default)
        {
            return await _context.RecruitmentRequests
                .Include(r => r.Department)
                    .ThenInclude(d => d!.Manager)
                        .ThenInclude(m => m!.Account)
                .Include(r => r.Position)
                .Include(r => r.Candidates)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }
    }
}
