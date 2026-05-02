using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Recruitment
{
    public class RecruitmentRepository : BaseRepository<RecruitmentRequest>, IRecruitmentRepository
    {
        public RecruitmentRepository(MyDbContext context) : base(context) { }

        // ==========================================
        // 1. RECRUITMENT REQUEST
        // ==========================================
        public async Task<RecruitmentRequest?> GetRequestWithDetailsAsync(int id)
        {
            // Lấy thông tin Yêu cầu tuyển dụng kèm theo danh sách Ứng viên đã nộp
            return await _dbSet
                .Include(r => r.Candidates)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task UpdateRequestStatusAsync(int id, RecruitmentRequestStatus status)
        {
            var request = await _dbSet.FindAsync(id);
            if (request != null)
            {
                request.Status = status; // Trạng thái: Pending, Approved, Rejected...
            }
        }

        // ==========================================
        // 2. CANDIDATE
        // ==========================================
        public async Task SaveCandidateAsync(Candidate candidate)
        {
            // Thêm mới ứng viên. 
            // Lưu ý: ID của Candidate sẽ tự động được EF Core sinh ra và gán vào object 'candidate' 
            // SAU KHI tầng UseCase gọi Unit_Of_Work.CommitAsync().
            await _context.Candidates.AddAsync(candidate);
        }

        public async Task<bool> IsRequestOpenForHiringAsync(int requestId)
        {
            // Job chỉ nhận CV khi trạng thái Yêu cầu là Approved (Đã duyệt)
            var request = await _dbSet.FindAsync(requestId);
            return request != null && request.Status == RecruitmentRequestStatus.Approved;
        }

        public async Task<bool> CheckExistingApplicationAsync(int requestId, string email)
        {
            // Chống spam: Kiểm tra xem email này đã nộp CV vào Job này chưa
            return await _context.Candidates
                .AnyAsync(c => c.RecruitmentRequestId == requestId && c.Email == email);
        }

        public async Task UpdateCandidateStatusAsync(int candidateId, CandidateStatus status, DateTime? deadline = null)
        {
            var candidate = await _context.Candidates.FindAsync(candidateId);
            if (candidate != null)
            {
                candidate.Status = status; // Trạng thái: Applied, Interviewing, Offered...
            }
        }

        public async Task<Candidate?> GetCandidateByIdAsync(int candidateId)
        {
            return await _context.Candidates.FindAsync(candidateId);
        }
    }
}
