using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IAccountManagementUseCase
    {
        Task<CreateAccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default);
        Task ToggleAccountStatusAsync(int accountId, AccountStatus newStatus, CancellationToken ct = default);
        Task<ResetPasswordResultDto> ResetPasswordManuallyAsync(int accountId, CancellationToken ct = default);
        Task UpdateAccountRoleAsync(int accountId, int newRoleId, CancellationToken ct = default);
        Task UpdateAccountRoleAsync(int targetAccountId, int newRoleId, int actorId, CancellationToken ct = default);
        Task<IEnumerable<AccountListItemDto>> GetAllAccountsAsync(CancellationToken ct = default);
    }
}
