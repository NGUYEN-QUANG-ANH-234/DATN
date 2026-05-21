using HRM.backend.src.HRM.Application.DTOs.Organization;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class OrgTreeUseCase : IOrgTreeUseCase
    {
        private readonly IDepartmentRepository _deptRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public OrgTreeUseCase(
            IDepartmentRepository deptRepo,
            IEmployeeRepository employeeRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _deptRepo = deptRepo;
            _employeeRepo = employeeRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<List<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken ct = default)
        {
            // 1. fetchAllActiveDepartments
            var allDepts = await _deptRepo.GetAllActiveAsync(ct);

            // 2. buildTreeStructure (O(N) Complexity)
            var lookup = allDepts.ToDictionary(d => d.Id, d => new DepartmentTreeDto
            {
                Id = d.Id,
                DeptCode = d.DeptCode,
                DeptName = d.DeptName,
                ParentDeptId = d.ParentDeptId,
                ManagerId = d.ManagerId,
                Status = d.Status.ToString()
            });

            var rootNodes = new List<DepartmentTreeDto>();

            foreach (var node in lookup.Values)
            {
                if (node.ParentDeptId.HasValue && lookup.ContainsKey(node.ParentDeptId.Value))
                {
                    lookup[node.ParentDeptId.Value].Children.Add(node);
                }
                else
                {
                    rootNodes.Add(node);
                }
            }

            return rootNodes;
        }

        public async Task<bool> UpdateDepartmentNodeAsync(int deptId, UpdateDeptStructureDto dto, int actorId, CancellationToken ct = default)
        {
            // Khóa luồng Tree Update để chống Race Condition khi nhiều Admin cùng kéo thả sơ đồ
            return await _lockService.GetWithLockAsync("org_tree_update", async (innerCt) =>
            {
                if (deptId == dto.NewParentId)
                    throw new InvalidOperationException("Một phòng ban không thể làm con của chính nó.");

                var currentDept = await _deptRepo.GetByIdAsync(deptId, innerCt);
                if (currentDept == null)
                    throw new KeyNotFoundException("Phòng ban không tồn tại.");

                // 1. checkCircularDependency
                if (dto.NewParentId.HasValue)
                {
                    int? currentParentCheckId = dto.NewParentId;
                    while (currentParentCheckId.HasValue)
                    {
                        if (currentParentCheckId.Value == deptId)
                            throw new InvalidOperationException("Lỗi Circular Dependency: Phòng ban cha mới đang là con/cháu của phòng ban hiện tại.");

                        var ancestor = await _deptRepo.GetByIdAsync(currentParentCheckId.Value, innerCt);
                        currentParentCheckId = ancestor?.ParentDeptId;
                    }
                }

                // 2. saveDepartment
                currentDept.ParentDeptId = dto.NewParentId;

                // 3. logAction
                await _auditLogRepo.LogSystemEventAsync(
                    actionType: "UPDATE_ORG_TREE",
                    accountId: actorId,
                    module: "departments",
                    message: $"Đổi phòng ban cha của DeptID {deptId} thành {dto.NewParentId}"
                );

                // 4. commitAsync
                await _unitOfWork.CommitAsync(innerCt);

                return true;
            }, TimeSpan.FromSeconds(10), ct);
        }

        public async Task<bool> DeactivateDepartmentAsync(int deptId, int actorId, CancellationToken ct = default)
        {
            // Khóa trên đúng ID phòng ban đang bị thao tác
            return await _lockService.GetWithLockAsync($"dept_deactivate_{deptId}", async (innerCt) =>
            {
                // 1. countActiveEmployeesInDept
                var activeEmployeesCount = await _employeeRepo.CountActiveInDeptAsync(deptId, innerCt);

                // Kiểm tra an toàn trước khi giải thể (Fail-fast)
                if (activeEmployeesCount > 0)
                {
                    throw new InvalidOperationException($"Không thể giải thể. Phòng ban này còn {activeEmployeesCount} nhân sự chưa luân chuyển.");
                }

                var hasSubDepartments = await _deptRepo.HasActiveSubDepartmentsAsync(deptId, innerCt);
                if (hasSubDepartments)
                {
                    throw new InvalidOperationException("Không thể giải thể phòng ban này khi các phòng ban con trực thuộc vẫn đang hoạt động.");
                }

                // 2. updateStatus
                var department = await _deptRepo.GetByIdAsync(deptId, innerCt);
                if (department == null) throw new KeyNotFoundException("Phòng ban không tồn tại.");

                department.Status = DeptStatus.Dissolved;

                // 3. logAction
                await _auditLogRepo.LogSystemEventAsync(
                    actionType: "DEACTIVATE_DEPARTMENT",
                    accountId: actorId,
                    module: "departments",
                    message: $"Ngừng hoạt động phòng ban {department.DeptCode}"
                );

                // 4. commitAsync
                await _unitOfWork.CommitAsync(innerCt);

                return true;
            }, TimeSpan.FromSeconds(10), ct);
        }

        public async Task<int> CreateDepartmentAsync(CreateDepartmentDto dto, int actorId, CancellationToken ct = default)
        {
            // Gọi hàm từ Repo thay vì dùng Query().AnyAsync()
            var exists = await _deptRepo.CheckCodeExistsAsync(dto.DeptCode, ct);
            if (exists)
            {
                throw new InvalidOperationException("Mã phòng ban đã tồn tại trên hệ thống.");
            }

            var newDept = new Department
            {
                DeptCode = dto.DeptCode,
                DeptName = dto.DeptName,
                ParentDeptId = dto.ParentDeptId,
                Status = DeptStatus.Active
            };

            await _deptRepo.AddAsync(newDept, ct);

            // Đồng bộ tên tham số AuditLog chuẩn xác
            await _auditLogRepo.LogSystemEventAsync(
                actionType: "CREATE_DEPARTMENT",
                accountId: actorId,
                module: "departments",
                message: $"Tạo mới phòng ban {dto.DeptName} ({dto.DeptCode})"
            );

            await _unitOfWork.CommitAsync(ct);

            return newDept.Id;
        }
    }
}
