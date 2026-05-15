using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class RbacRepository : IRbacRepository
    {
        private readonly MyDbContext _context;

        public RbacRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoleWithPermissionsDto>> FetchRolesWithPermissionsAsync(CancellationToken ct = default)
        {
            // Truy vấn xuyên qua bảng trung gian RolePermissions để lấy PermissionCode
            return await _context.Set<Role>()
                .AsNoTracking()
                .Select(r => new RoleWithPermissionsDto
                {
                    RoleId = r.Id,
                    RoleName = r.RoleName,
                    Permissions = r.RolePermissions.Select(rp => rp.Permission.PermissionCode).ToList()
                })
                .ToListAsync(ct);
        }

        public async Task UpdateRolePermissionsAsync(int roleId, IEnumerable<string> permissionCodes, CancellationToken ct = default)
        {
            // 1. Lấy danh sách ID của các mã quyền mới từ bảng permissions
            var newPermissionIds = await _context.Set<Permission>()
                .Where(p => permissionCodes.Contains(p.PermissionCode))
                .Select(p => p.Id)
                .ToListAsync(ct);

            // 2. Lấy danh sách quyền hiện tại của Role trong bảng trung gian
            var currentRolePermissions = await _context.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync(ct);

            // 3. TÌM VÀ XÓA: Các quyền cũ không còn nằm trong danh sách mới gửi lên
            var toRemove = currentRolePermissions
                .Where(rp => !newPermissionIds.Contains(rp.PermissionId))
                .ToList();

            if (toRemove.Any())
            {
                _context.Set<RolePermission>().RemoveRange(toRemove);
            }

            // 4. TÌM VÀ THÊM: Các quyền mới chưa có trong DB
            var currentPermissionIds = currentRolePermissions.Select(rp => rp.PermissionId).ToList();
            var toAddIds = newPermissionIds.Where(id => !currentPermissionIds.Contains(id)).ToList();

            if (toAddIds.Any())
            {
                var toAdd = toAddIds.Select(id => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = id
                });
                await _context.Set<RolePermission>().AddRangeAsync(toAdd, ct);
            }
        }

        public async Task<IEnumerable<string>> GetAllPermissionCodesAsync(CancellationToken ct = default)
        {
            return await _context.Set<Permission>()
                .AsNoTracking()
                .Select(p => p.PermissionCode)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<PermissionGroupDto>> GetGroupedPermissionsAsync(CancellationToken ct = default)
        {
            return await _context.Set<Permission>()
                .AsNoTracking()
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroupDto
                {
                    Group = string.IsNullOrEmpty(g.Key) ? "Chưa phân loại" : g.Key,
                    Codes = g.Select(p => new PermissionItemDto
                    {
                        Code = p.PermissionCode,
                        Desc = p.Description
                    }).ToList()
                })
                .ToListAsync(ct);
        }
    }
}
