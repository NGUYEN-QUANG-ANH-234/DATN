using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases
{
    public interface IPromotionOfficialUseCase
    {
        Task<PersonnelChangeDetailDto> CreatePromotionAsync(CreatePromotionDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> CreateConvertOfficialAsync(CreateConvertOfficialDto dto, int actorAccountId, CancellationToken ct);
        Task<PersonnelChangeDetailDto> HrReviewPromotionAsync(int id, int actorAccountId, ApprovePromotionDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> DirectorApprovePromotionAsync(int id, int actorAccountId, ApprovePromotionDto dto, CancellationToken ct);
        Task<PersonnelChangeDetailDto> ExecutePromotionAsync(int id, int actorAccountId, ExecutePersonnelChangeDto dto, CancellationToken ct);
    }
}
