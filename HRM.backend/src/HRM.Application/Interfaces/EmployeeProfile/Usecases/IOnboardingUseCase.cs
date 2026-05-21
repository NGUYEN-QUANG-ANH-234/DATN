using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IOnboardingUseCase
    {
        Task SubmitProfileAsync(SubmitOnboardingDto dto, CancellationToken ct = default);
        Task ReviewByHrAsync(int requestId, ReviewOnboardingDto dto, CancellationToken ct = default);
    }
}
