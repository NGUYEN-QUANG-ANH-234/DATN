using HRM.backend.src.HRM.Application.DTOs.Recruitment;

namespace HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases
{
    public interface ICandidateUseCase
    {
        Task<ApplyJobResultDto> ApplyForJobAsync(ApplyJobDto dto, CancellationToken ct = default);
        Task<IEnumerable<CandidateHistoryDto>> GetMyApplicationsAsync(string email, string trackingCode, CancellationToken ct = default);
        Task<IEnumerable<CandidateHistoryDto>> GetAllCandidatesAsync(int userId, string actorRoleName, CancellationToken ct = default);
        Task<bool> HrApproveAsync(int candidateId, int actorId, string actorRoleName, CancellationToken ct = default);
        Task<bool> ConfirmByDepartmentAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default);
        Task<bool> FinalApproveAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default);
        Task<bool> RejectAsync(int candidateId, int actorId, string actorRoleName, CancellationToken ct = default);
    }
}
