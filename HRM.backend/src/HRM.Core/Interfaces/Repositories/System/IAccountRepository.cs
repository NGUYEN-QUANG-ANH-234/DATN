using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IAccountRepository : IBaseRepository<Account>
    {
        // --- IRBAC_Repo ---
        Task<IEnumerable<Role>> FetchRolesWithPermissionMatrixAsync();
        Task UpdateRolePermissionMappingAsync(int roleId, List<int> permissionIds);

        // --- IUser_Repo ---
        Task<Account> FindOrUpsertUserAsync(string email, string name, string? oauthId = null);
        Task InsertUserAsync(Account user);
        Task UpdateStatusAsync(int id, AccountStatus status); // status: Active/Inactive
        Task UpdateHashedPasswordAsync(int id, string hashedPassword);
    }
}
