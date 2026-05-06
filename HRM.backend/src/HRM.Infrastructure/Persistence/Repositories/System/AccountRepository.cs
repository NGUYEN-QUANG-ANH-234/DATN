using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class AccountRepository : BaseRepository<Account>, IAccountRepository
    {
        public AccountRepository(MyDbContext context) : base(context) { }

        // ==========================================
        // 1. RBAC (Phân quyền)
        // ==========================================
        public async Task<IEnumerable<Role>> FetchRolesWithPermissionMatrixAsync()
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();
        }

        public async Task UpdateRolePermissionMappingAsync(int roleId, List<int> permissionIds)
        {
            var existingMappings = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _context.RolePermissions.RemoveRange(existingMappings);

            var newMappings = permissionIds.Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid });
            await _context.RolePermissions.AddRangeAsync(newMappings);
        }

        // ==========================================
        // 2. USER (Quản lý tài khoản)
        // ==========================================
        public async Task<Account> FindOrUpsertUserAsync(string email, string name, string? oauthId = null)
        {
            var user = await _dbSet.FirstOrDefaultAsync(a => a.Email == email);
            if (user == null)
            {
                user = new Account
                {
                    Email = email,
                    OAuthId = oauthId,
                    RoleId = 7,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbSet.AddAsync(user);
            }
            return user;
        }

        public async Task InsertUserAsync(Account user)
        {
            await _dbSet.AddAsync(user);
        }

        public async Task UpdateStatusAsync(int id, AccountStatus status)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null) user.Status = status;
        }

        public async Task UpdateHashedPasswordAsync(int id, string hashedPassword)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null) user.PasswordHash = hashedPassword;
        }
    }
}
