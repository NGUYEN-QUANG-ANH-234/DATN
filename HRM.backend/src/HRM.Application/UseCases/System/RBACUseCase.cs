using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class RbacUseCase : IRbacUseCase
    {
        private readonly IRbacRepository _rbacRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly IRoleRepository _roleRepo;
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "RBAC_Matrix_Cache";
        private const string ALL_PERMISSIONS_CACHE_KEY = "All_Permissions_Cache";

        public RbacUseCase(
            IRbacRepository rbacRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            IRoleRepository roleRepo,
            ILockService lockService)
        {
            _rbacRepo = rbacRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _roleRepo = roleRepo;
            _lockService = lockService;
        }

        public async Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesAndPermissionsAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var matrix = (await _rbacRepo.FetchRolesWithPermissionsAsync(innerCt)).ToList();
                    var allPermissionCodes = (await _rbacRepo.GetAllPermissionCodesAsync(innerCt)).ToList();
                    var adminRole = matrix.FirstOrDefault(r =>
                        string.Equals(r.RoleName, "Admin", StringComparison.OrdinalIgnoreCase));

                    if (adminRole != null)
                    {
                        adminRole.Permissions = allPermissionCodes;
                    }

                    return matrix;
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto, int adminId, CancellationToken ct = default)
        {
            var targetRole = (await _roleRepo.GetAllRolesAsync(ct))
                .FirstOrDefault(r => r.Id == dto.RoleId);

            if (targetRole != null &&
                string.Equals(targetRole.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Hành động bị từ chối: Không thể thay đổi hay xóa quyền của Super Admin (Root).");

            bool isSuccess = false;

            await _lockService.GetWithLockAsync($"rbac_role_{dto.RoleId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    await _rbacRepo.UpdateRolePermissionsAsync(dto.RoleId, dto.PermissionCodes, innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, ct);
            }

            return isSuccess;
        }

        public async Task<IEnumerable<PermissionGroupDto>> GetAllAvailablePermissionsAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                ALL_PERMISSIONS_CACHE_KEY,
                async (innerCt) => await _rbacRepo.GetGroupedPermissionsAsync(innerCt),
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<IEnumerable<RoleDto>> GetSystemRolesAsync(CancellationToken ct = default)
        {
            var roles = await _roleRepo.GetAllRolesAsync(ct);

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.RoleName,
                Description = r.Description
            });
        }
    }
}
