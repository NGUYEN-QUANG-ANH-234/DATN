using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment
{
    public interface IRecruitmentRepository : IBaseRepository<RecruitmentRequest>
    {
        // ==========================================
        // 1. RECRUITMENT REQUEST (Yêu cầu tuyển dụng)
        // ==========================================
        Task<RecruitmentRequest?> GetRequestWithDetailsAsync(int id);
        Task UpdateRequestStatusAsync(int id, RecruitmentRequestStatus status);

        // ==========================================
        // 2. CANDIDATE (Ứng viên)
        // ==========================================
        Task SaveCandidateAsync(Candidate candidate);
        Task<bool> IsRequestOpenForHiringAsync(int requestId); // Ánh xạ từ GetJobStatus
        Task<bool> CheckExistingApplicationAsync(int requestId, string email);
        Task UpdateCandidateStatusAsync(int candidateId, CandidateStatus status, DateTime? deadline = null);
        Task<Candidate?> GetCandidateByIdAsync(int candidateId); // Dùng chung cho việc lấy DeadlineInfo & activateHiring
    }
}
