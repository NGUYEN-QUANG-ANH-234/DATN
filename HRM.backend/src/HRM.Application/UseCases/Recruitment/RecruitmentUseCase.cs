using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
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
        private readonly IAccountRepository _accountRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IApprovalWorkflowService _approvalService;
        private readonly ISlaTrackingService _slaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;

        public RecruitmentUseCase(
            IRecruitmentRequestRepository reqRepo,
            IDepartmentRepository deptRepo,
            IPositionRepository positionRepo,
            IAccountRepository accountRepo,
            IEmployeeRepository employeeRepo,
            IApprovalWorkflowService approvalService,
            ISlaTrackingService slaService,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IIdempotencyService idempotencyService)
        {
            _reqRepo = reqRepo;
            _deptRepo = deptRepo;
            _positionRepo = positionRepo;
            _accountRepo = accountRepo;
            _employeeRepo = employeeRepo;
            _approvalService = approvalService;
            _slaService = slaService;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
        }

        public async Task<int> CreateRequestAsync(CreateRecruitmentDto dto, int creatorId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("RECRUITMENT_REQUEST_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
                return existingResourceId.Value;

            if (IsManager(actorRoleName))
            {
                var managerDeptId = await GetManagerDeptIdAsync(creatorId, ct);
                if (dto.DeptId.HasValue && dto.DeptId.Value != managerDeptId)
                    throw new UnauthorizedAccessException("Manager chỉ được tạo yêu cầu tuyển dụng cho phòng ban của mình.");

                dto.DeptId = managerDeptId;
            }

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

            var lockDeptId = dto.DeptId?.ToString() ?? "none";
            var lockPositionId = dto.PositionId?.ToString() ?? "none";
            return await _lockService.GetWithLockAsync($"recruitment_create_{creatorId}_{lockDeptId}_{lockPositionId}", async (innerCt) =>
            {
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

                await _reqRepo.AddAsync(request, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                var hrAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("HR", innerCt);
                var directorAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", innerCt);

                if (!hrAccountIds.Any() || !directorAccountIds.Any())
                    throw new InvalidOperationException("Hệ thống chưa thiết lập tài khoản HR hoặc Director để duyệt yêu cầu này.");

                var approvers = new List<int> { hrAccountIds.First(), directorAccountIds.First() };
                await _approvalService.CreateWorkflowAsync("RECRUITMENT", request.Id, approvers, innerCt);
                await _slaService.CreateTaskAsync(SlaModuleType.Recruitment, request.Id, innerCt);
                await _idempotencyService.SaveAsync("RECRUITMENT_REQUEST_CREATE", idempotencyKey ?? string.Empty, "RecruitmentRequest", request.Id, creatorId, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                return request.Id;
            }, cancellationToken: ct);
        }

        public async Task<bool> ReviewRequestAsync(
            int requestId,
            int approverId,
            string actorRoleName,
            ReviewRecruitmentDto dto,
            CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"recruitment_request_{requestId}", async (innerCt) =>
            {
                var request = await _reqRepo.GetByIdAsync(requestId, innerCt);
                if (request == null)
                    throw new InvalidOperationException("Yêu cầu không tồn tại.");

                var workflowStatus = await _approvalService.ProcessStepAsync(
                    "RECRUITMENT",
                    requestId,
                    approverId,
                    actorRoleName,
                    dto.IsApproved,
                    dto.Note,
                    innerCt);

                if (workflowStatus == ApprovalStatus.Approved)
                {
                    request.Status = RecruitmentRequestStatus.Approved;
                    await _reqRepo.UpdateAsync(request, innerCt);
                    await _slaService.ResolveTaskAsync(SlaModuleType.Recruitment, requestId, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                }
                else if (workflowStatus == ApprovalStatus.Rejected)
                {
                    request.Status = RecruitmentRequestStatus.Rejected;
                    await _reqRepo.UpdateAsync(request, innerCt);
                    await _slaService.ResolveTaskAsync(SlaModuleType.Recruitment, requestId, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                }
                else if (workflowStatus == ApprovalStatus.Pending &&
                         string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase) &&
                         dto.IsApproved)
                {
                    request.Status = RecruitmentRequestStatus.PendingDirector;
                    await _reqRepo.UpdateAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                }

                return true;
            }, cancellationToken: ct);
        }

        public async Task<List<RecruitmentRequest>> GetPendingRequestsAsync(int actorId, CancellationToken ct = default)
        {
            var account = await _accountRepo.GetByIdAsync(actorId, ct);
            if (account == null || account.Role == null)
                return new List<RecruitmentRequest>();

            if (account.Role.RoleName == "HR")
                return await _reqRepo.GetRequestsByStatusAsync(RecruitmentRequestStatus.PendingHR, ct);

            if (account.Role.RoleName == "Director")
                return await _reqRepo.GetRequestsByStatusAsync(RecruitmentRequestStatus.PendingDirector, ct);

            return new List<RecruitmentRequest>();
        }

        public async Task<List<RecruitmentRequest>> GetMyRequestsAsync(int userId, CancellationToken ct = default)
        {
            return await _reqRepo.GetRequestsByCreatorAsync(userId, ct);
        }

        public async Task<List<RecruitmentRequestListItemDto>> GetRequestsAsync(int actorId, string actorRoleName, CancellationToken ct = default)
        {
            var requests = await _reqRepo.GetRequestsWithCandidatesAsync(ct);

            if (IsManager(actorRoleName))
            {
                var managerDeptId = await GetManagerDeptIdAsync(actorId, ct);
                requests = requests
                    .Where(r => r.DeptId.HasValue && r.DeptId.Value == managerDeptId)
                    .ToList();
            }
            else if (!CanViewAllRecruitmentRequests(actorRoleName))
            {
                requests = requests
                    .Where(r => r.CreatedById == actorId)
                    .ToList();
            }

            var changed = false;
            foreach (var request in requests)
            {
                if (request.Status == RecruitmentRequestStatus.Approved && IsRequestFull(request))
                {
                    request.Status = RecruitmentRequestStatus.Closed;
                    _reqRepo.Update(request);
                    changed = true;
                }
            }

            if (changed)
                await _unitOfWork.CommitAsync(ct);

            return requests.Select(MapToListItem).ToList();
        }

        public async Task<RecruitmentRequestListItemDto> CloseRequestAsync(int requestId, int actorId, string actorRoleName, CloseRecruitmentRequestDto dto, CancellationToken ct = default)
        {
            if (!CanCloseRecruitmentRequest(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được đóng tin tuyển dụng.");

            return await _lockService.GetWithLockAsync($"recruitment_close_{requestId}", async (innerCt) =>
            {
                var request = await _reqRepo.GetByIdWithCandidatesAsync(requestId, innerCt);
                if (request == null)
                    throw new InvalidOperationException("Không tìm thấy nhu cầu tuyển dụng.");

                if (request.Status == RecruitmentRequestStatus.Closed)
                    throw new InvalidOperationException("Tin tuyển dụng đã được đóng trước đó.");

                if (request.Status != RecruitmentRequestStatus.Approved)
                    throw new InvalidOperationException("Chỉ có thể đóng tin tuyển dụng đang mở.");

                request.Status = RecruitmentRequestStatus.Closed;
                _reqRepo.Update(request);
                await _unitOfWork.CommitAsync(innerCt);

                return MapToListItem(request);
            }, cancellationToken: ct);
        }

        private static bool IsManager(string actorRoleName)
        {
            return string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanViewAllRecruitmentRequests(string actorRoleName)
        {
            return string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanCloseRecruitmentRequest(string actorRoleName)
        {
            return string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase);
        }

        private static RecruitmentRequestListItemDto MapToListItem(RecruitmentRequest request)
        {
            var filledSlots = CountFilledSlots(request);
            var activeCandidateCount = CountActiveCandidates(request);
            var isExpired = IsExpired(request);
            var isFull = request.Quantity > 0 && filledSlots >= request.Quantity;
            var isClosed = request.Status == RecruitmentRequestStatus.Closed;

            return new RecruitmentRequestListItemDto
            {
                Id = request.Id,
                Quantity = request.Quantity,
                FilledSlots = filledSlots,
                ActiveCandidateCount = activeCandidateCount,
                RemainingSlots = Math.Max(request.Quantity - filledSlots, 0),
                Description = request.Description,
                Deadline = request.Deadline,
                CreatedAt = request.CreatedAt,
                Status = request.Status.ToString(),
                DepartmentName = request.Department?.DeptName,
                PositionName = request.Position?.Title,
                IsClosed = isClosed,
                IsExpired = isExpired,
                IsFull = isFull,
                CanApply = request.Status == RecruitmentRequestStatus.Approved && !isClosed && !isExpired && !isFull
            };
        }

        private static int CountFilledSlots(RecruitmentRequest request)
        {
            return request.Candidates.Count(c => c.Status == CandidateStatus.Offer || c.Status == CandidateStatus.Hired);
        }

        private static int CountActiveCandidates(RecruitmentRequest request)
        {
            return request.Candidates.Count(c => c.Status != CandidateStatus.Rejected && c.Status != CandidateStatus.SLA_Expired);
        }

        private static bool IsRequestFull(RecruitmentRequest request)
        {
            return request.Quantity > 0 && CountFilledSlots(request) >= request.Quantity;
        }

        private static bool IsExpired(RecruitmentRequest request)
        {
            return request.Deadline.HasValue && request.Deadline.Value.Date < DateTime.UtcNow.Date;
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
