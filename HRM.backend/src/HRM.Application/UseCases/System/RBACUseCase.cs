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

        private const string CACHE_KEY = "RBAC_Matrix_Cache";
        private const string ALL_PERMISSIONS_CACHE_KEY = "All_Permissions_Cache";

        public RbacUseCase(
            IRbacRepository rbacRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            IRoleRepository roleRepo)
        {
            _rbacRepo = rbacRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _roleRepo = roleRepo;
        }

        public async Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesAndPermissionsAsync(CancellationToken ct = default)
        {
            var cachedMatrix = await _cache.GetAsync<IEnumerable<RoleWithPermissionsDto>>(CACHE_KEY);
            if (cachedMatrix != null) return cachedMatrix;

            var matrix = (await _rbacRepo.FetchRolesWithPermissionsAsync(ct)).ToList();
            var allPermissionCodes = (await _rbacRepo.GetAllPermissionCodesAsync(ct)).ToList();
            var adminRole = matrix.FirstOrDefault(r => r.RoleId == 1);

            if (adminRole != null)
            {
                adminRole.Permissions = allPermissionCodes;
            }

            await _cache.SetAsync(CACHE_KEY, matrix, TimeSpan.FromHours(24), null, ct);
            return matrix;
        }

        public async Task<bool> UpdateRolePermissionsAsync(UpdateRolePermissionsDto dto, int adminId, CancellationToken ct = default)
        {
            if (dto.RoleId == 1)
                throw new ArgumentException("Hành động bị từ chối: Không thể thay đổi hay xóa quyền của Super Admin (Root).");

            bool isSuccess = false;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _rbacRepo.UpdateRolePermissionsAsync(dto.RoleId, dto.PermissionCodes, ct);

                // Ghi log đã được chuyển giao cho DbContext Hook lo liệu

                await _unitOfWork.CommitAsync(ct);
                isSuccess = true;
            }, ct);

            if (isSuccess)
            {
                await _cache.RemoveAsync(CACHE_KEY, ct);
            }

            return isSuccess;
        }

        public async Task<IEnumerable<PermissionGroupDto>> GetAllAvailablePermissionsAsync(CancellationToken ct = default)
        {
            var cachedPermissions = await _cache.GetAsync<IEnumerable<PermissionGroupDto>>(ALL_PERMISSIONS_CACHE_KEY);
            if (cachedPermissions != null) return cachedPermissions;

            var permissions = await _rbacRepo.GetGroupedPermissionsAsync(ct);
            await _cache.SetAsync(ALL_PERMISSIONS_CACHE_KEY, permissions, TimeSpan.FromHours(24), null, ct);

            return permissions;
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