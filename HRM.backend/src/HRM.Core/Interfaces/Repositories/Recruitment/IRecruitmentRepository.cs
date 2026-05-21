using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment
{
    public interface IRecruitmentRequestRepository : IBaseRepository<RecruitmentRequest>
    {
        Task<List<RecruitmentRequest>> GetActiveJobPostingsAsync(CancellationToken ct = default);
        Task<List<RecruitmentRequest>> GetRequestsByStatusAsync(RecruitmentRequestStatus status, CancellationToken ct = default);
        Task<List<RecruitmentRequest>> GetRequestsByCreatorAsync(int userId, CancellationToken ct = default);
        Task<List<RecruitmentRequest>> GetRequestsWithDetailsAsync(List<int> ids, CancellationToken ct = default);
    }
}
