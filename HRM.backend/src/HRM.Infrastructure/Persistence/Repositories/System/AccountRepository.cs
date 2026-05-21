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
        public async Task<IEnumerable<Role>> FetchRolesWithPermissionMatrixAsync(CancellationToken ct = default)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();
        }

        public async Task UpdateRolePermissionMappingAsync(int roleId, List<int> permissionIds, CancellationToken ct = default)
        {
            var existingMappings = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _context.RolePermissions.RemoveRange(existingMappings);

            var newMappings = permissionIds.Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid });
            await _context.RolePermissions.AddRangeAsync(newMappings);
        }

        // ==========================================
        // 2. USER (Quản lý tài khoản)
        // ==========================================
        public async Task<Account> FindOrUpsertUserAsync(string email, string fullName, string? avatarUrl = null, string? oauthId = null, CancellationToken ct = default)
        {
            var user = await _dbSet.FirstOrDefaultAsync(a => a.Email == email, ct);

            if (user == null)
            {
                user = new Account
                {
                    Email = email,
                    FullName = fullName,
                    AvatarUrl = avatarUrl,
                    OAuthId = oauthId,
                    RoleId = 8, // Default Role
                    CreatedAt = DateTime.UtcNow
                };
                await _dbSet.AddAsync(user, ct);
            }
            else
            {
                // CẬP NHẬT thông tin mới nếu đã tồn tại
                user.FullName = fullName;
                user.AvatarUrl = avatarUrl;
                if (!string.IsNullOrEmpty(oauthId)) user.OAuthId = oauthId;

                _dbSet.Update(user);
            }
            return user;
        }

        public async Task InsertUserAsync(Account user, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(user);
        }

        public async Task UpdateStatusAsync(int id, AccountStatus status, CancellationToken ct = default)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null) user.Status = status;
        }

        public async Task UpdateHashedPasswordAsync(int id, string hashedPassword, CancellationToken ct = default)
        {
            var user = await _dbSet.FindAsync(id);
            if (user != null) user.PasswordHash = hashedPassword;
        }

        public async Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(a => a.Email == email, ct);
        }

        public async Task<List<Account>> GetAllWithRoleAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Role)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<int>> GetAccountIdsByRoleAsync(string roleName, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(a => a.Role)
                .Where(a => a.Role != null &&
                            a.Role.RoleName == roleName &&
                            a.Status == AccountStatus.Active)
                .Select(a => a.Id)
                .ToListAsync(ct);
        }
    }
}
