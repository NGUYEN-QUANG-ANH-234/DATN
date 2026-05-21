using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.Recruitment
{
    public class RecruitmentUseCase : IRecruitmentUseCase
    {
        private readonly IRecruitmentRequestRepository _reqRepo;
        private readonly IDepartmentRepository _deptRepo;
        private readonly IPositionRepository _positionRepo;
        private readonly IAccountRepository _accountRepo; // TIÊM REPO QUẢN LÝ TÀI KHOẢN
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IApprovalWorkflowService _approvalService;
        private readonly ISlaTrackingService _slaService;
        private readonly IUnitOfWork _unitOfWork;

        public RecruitmentUseCase(
            IRecruitmentRequestRepository reqRepo,
            IDepartmentRepository deptRepo,
            IPositionRepository positionRepo,
            IAccountRepository accountRepo, // Tiêm vào Constructor
            IEmployeeRepository employeeRepo,
            IApprovalWorkflowService approvalService,
            ISlaTrackingService slaService,
            IUnitOfWork unitOfWork)
        {
            _reqRepo = reqRepo;
            _deptRepo = deptRepo;
            _positionRepo = positionRepo;
            _accountRepo = accountRepo;
            _employeeRepo = employeeRepo;
            _approvalService = approvalService;
            _slaService = slaService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateRequestAsync(CreateRecruitmentDto dto, int creatorId, string actorRoleName, CancellationToken ct = default)
        {
            if (IsManager(actorRoleName))
            {
                var managerDeptId = await GetManagerDeptIdAsync(creatorId, ct);
                if (dto.DeptId.HasValue && dto.DeptId.Value != managerDeptId)
                    throw new UnauthorizedAccessException("Manager chỉ được tạo yêu cầu tuyển dụng cho phòng ban của mình.");

                dto.DeptId = managerDeptId;
            }

            // 1. Validate Phòng ban & Vị trí (Giữ nguyên như cũ)
            if (dto.DeptId.HasValue)
            {
                var dept = await _deptRepo.GetByIdAsync(dto.DeptId.Value, ct);
                if (dept == null || dept.Status != DeptStatus.Active)
                    throw new InvalidOperationException("Phòng ban không tồn tại hoặc đã bị giải thể.");
            }

            if (dto.PositionId.HasValue)
            {
                var position = await _positionRepo.GetByIdAsync(dto.PositionId.Value, ct);
                if (position == null || !position.IsActive)
                    throw new InvalidOperationException("Vị trí chức danh không tồn tại hoặc đã ngừng sử dụng.");
            }

            // 2. Lưu Request
            var request = new RecruitmentRequest
            {
                DeptId = dto.DeptId,
                PositionId = dto.PositionId,
                Quantity = dto.Quantity,
                Description = dto.Description,
                Deadline = dto.Deadline,
                Status = RecruitmentRequestStatus.PendingHR,
                CreatedById = creatorId
            };


            await _reqRepo.AddAsync(request, ct);
            await _unitOfWork.CommitAsync(ct); // Chốt lần 1 để lấy request.Id thật

            // =========================================================
            // 3. TỰ ĐỘNG XÁC ĐỊNH NGƯỜI DUYỆT (AUTO-ROUTING WORKFLOW)
            // =========================================================

            // Tìm những tài khoản đang giữ Role là HR và Director
            var hrAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("HR", ct);
            var directorAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);

            if (!hrAccountIds.Any() || !directorAccountIds.Any())
                throw new InvalidOperationException("Hệ thống chưa thiết lập tài khoản HR Manager hoặc Director để duyệt yêu cầu này.");

            // Lấy ID đầu tiên tìm được để đưa vào luồng duyệt
            List<int> approvers = new List<int>
            {
                hrAccountIds.First(),
                directorAccountIds.First()
            };

            // Truyền mảng approvers tự động sinh này cho Workflow Engine
            await _approvalService.CreateWorkflowAsync("RECRUITMENT", request.Id, approvers, ct);
            // =========================================================

            // 4. Gọi Service SLA Trung tâm 
            await _slaService.CreateTaskAsync(SlaModuleType.Recruitment, request.Id, ct);

            await _unitOfWork.CommitAsync(ct);
            return request.Id;
        }

        public async Task<bool> ReviewRequestAsync(
            int requestId,
            int approverId,
            string actorRoleName, // Thêm tham số nhóm quyền
            ReviewRecruitmentDto dto,
            CancellationToken ct = default)
        {
            var request = await _reqRepo.GetByIdAsync(requestId, ct);
            if (request == null) throw new InvalidOperationException("Yêu cầu không tồn tại.");

            // Truyền actorRoleName vào vị trí tham số thứ 3 theo thiết kế mới của ProcessStepAsync
            var workflowStatus = await _approvalService.ProcessStepAsync("RECRUITMENT", requestId, approverId, actorRoleName, dto.IsApproved, dto.Note, ct);
            
            // Xử lý cập nhật trạng thái của Yêu cầu tuyển dụng
            if (workflowStatus == ApprovalStatus.Approved)
            {
                request.Status = RecruitmentRequestStatus.Approved;
                await _reqRepo.UpdateAsync(request, ct);
                
                // Giải quyết SLA Task
                await _slaService.ResolveTaskAsync(SlaModuleType.Recruitment, requestId, ct);
                
                await _unitOfWork.CommitAsync(ct);
            }
            else if (workflowStatus == ApprovalStatus.Rejected)
            {
                request.Status = RecruitmentRequestStatus.Rejected;
                await _reqRepo.UpdateAsync(request, ct);
                
                // Giải quyết SLA Task
                await _slaService.ResolveTaskAsync(SlaModuleType.Recruitment, requestId, ct);
                
                await _unitOfWork.CommitAsync(ct);
            }
            else if (workflowStatus == ApprovalStatus.Pending)
            {
                // Nếu vẫn đang pending, kiểm tra xem nó có vừa pass cấp HR để lên Director không.
                if (actorRoleName == "HR" && dto.IsApproved)
                {
                    request.Status = RecruitmentRequestStatus.PendingDirector;
                    await _reqRepo.UpdateAsync(request, ct);
                    await _unitOfWork.CommitAsync(ct);
                }
            }

            return true;
        }
        public async Task<List<RecruitmentRequest>> GetPendingRequestsAsync(int actorId, CancellationToken ct = default)
        {
            // 1. Tìm tài khoản người dùng để lấy RoleName
            var account = await _accountRepo.GetByIdAsync(actorId, ct); // Giả định BaseRepository có GetByIdAsync
            if (account == null || account.Role == null)
                return new List<RecruitmentRequest>();

            // 2. Phân luồng dữ liệu dựa trên vai trò (Role)
            if (account.Role.RoleName == "HR")
            {
                return await _reqRepo.GetRequestsByStatusAsync(RecruitmentRequestStatus.PendingHR, ct);
            }
            else if (account.Role.RoleName == "Director")
            {
                return await _reqRepo.GetRequestsByStatusAsync(RecruitmentRequestStatus.PendingDirector, ct);
            }

            // Các vai trò khác (như Nhân viên, Trưởng phòng) không có quyền duyệt -> Trả về mảng rỗng
            return new List<RecruitmentRequest>();
        }

        public async Task<List<RecruitmentRequest>> GetMyRequestsAsync(int userId, CancellationToken ct = default)
        {
            return await _reqRepo.GetRequestsByCreatorAsync(userId, ct);
        }

        private static bool IsManager(string actorRoleName)
        {
            return string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int> GetManagerDeptIdAsync(int accountId, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null || !employee.DeptId.HasValue)
                throw new UnauthorizedAccessException("Tài khoản Manager chưa được gắn với phòng ban.");

            return employee.DeptId.Value;
        }
    }
}
