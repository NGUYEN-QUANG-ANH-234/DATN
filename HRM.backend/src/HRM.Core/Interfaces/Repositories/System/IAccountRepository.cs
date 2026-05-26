using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IAccountRepository : IBaseRepository<Account>
    {
        // --- IRBAC_Repo ---
        Task<IEnumerable<Role>> FetchRolesWithPermissionMatrixAsync(CancellationToken ct = default);
        Task UpdateRolePermissionMappingAsync(int roleId, List<int> permissionIds, CancellationToken ct = default);

        // --- IUser_Repo ---
        Task<Account> FindOrUpsertUserAsync(string email, string fullName, string? avatarUrl = null, string? oauthId = null, CancellationToken ct = default);
        Task InsertUserAsync(Account user, CancellationToken ct);
        Task UpdateStatusAsync(int id, AccountStatus status, CancellationToken ct = default); // status: Active/Inactive
        Task UpdateHashedPasswordAsync(int id, string hashedPassword, CancellationToken ct = default);
        Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<Account?> GetByIdWithRoleAsync(int id, CancellationToken ct = default);
        Task<List<Account>> GetAllWithRoleAsync(CancellationToken ct = default);
        Task<List<int>> GetAccountIdsByRoleAsync(string roleName, CancellationToken ct = default);
    }
}
