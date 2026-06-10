using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Core.Entities.Recruitment;

namespace HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases
{
    public interface IRecruitmentUseCase
    {
        Task<int> CreateRequestAsync(CreateRecruitmentDto dto, int creatorId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null);
        Task<bool> ReviewRequestAsync(
            int requestId,
            int approverId,
            string actorRoleName, // Thêm tham số nhóm quyền
            ReviewRecruitmentDto dto,
            CancellationToken ct = default);
        Task<List<RecruitmentRequest>> GetPendingRequestsAsync(int actorId, CancellationToken ct = default);
        Task<List<RecruitmentRequest>> GetMyRequestsAsync(int userId, CancellationToken ct = default);
        Task<List<RecruitmentRequestListItemDto>> GetRequestsAsync(int actorId, string actorRoleName, CancellationToken ct = default);
        Task<RecruitmentRequestListItemDto> CloseRequestAsync(int requestId, int actorId, string actorRoleName, CloseRecruitmentRequestDto dto, CancellationToken ct = default);
    }
}
