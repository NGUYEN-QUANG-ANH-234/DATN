using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using MediatR;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IBaseRepository<ApprovalRequest> _requestRepo;
        private readonly IBaseRepository<ApprovalStep> _stepRepo;
        private readonly IRecruitmentRequestRepository _reqRepo;
        private readonly ICandidateRepository _candidateRepo;
        private readonly IContractRepository _contractRepo;
        private readonly ILockService _lockService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public ApprovalWorkflowService(
            IBaseRepository<ApprovalRequest> requestRepo,
            IBaseRepository<ApprovalStep> stepRepo,
            IRecruitmentRequestRepository reqRepo,
            ICandidateRepository candidateRepo,
            IContractRepository contractRepo,
            ILockService lockService,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _requestRepo = requestRepo;
            _stepRepo = stepRepo;
            _reqRepo = reqRepo;
            _candidateRepo = candidateRepo;
            _contractRepo = contractRepo;
            _lockService = lockService;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<int> CreateWorkflowAsync(
            string moduleCode,
            int referenceId,
            List<int> approverAccountIds,
            CancellationToken ct = default)
        {
            if (approverAccountIds == null || !approverAccountIds.Any())
                throw new ArgumentException("Phai co it nhat mot nguoi duyet.");

            return await _lockService.GetWithLockAsync(
                LockKeys.Approval(moduleCode, referenceId),
                innerCt => CreateWorkflowCoreAsync(moduleCode, referenceId, approverAccountIds, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<int> CreateWorkflowCoreAsync(
            string moduleCode,
            int referenceId,
            List<int> approverAccountIds,
            CancellationToken ct = default)
        {
            if (approverAccountIds == null || !approverAccountIds.Any())
                throw new ArgumentException("Phải có ít nhất một người duyệt.");

            var expectedCurrentApproverId = approverAccountIds.First();
            var existingPendingRequests = (await _requestRepo.FindAsync(r =>
                r.ModuleCode == moduleCode &&
                r.ReferenceId == referenceId &&
                r.Status == ApprovalStatus.Pending, ct))
                .OrderByDescending(r => r.Id)
                .ToList();

            if (existingPendingRequests.Count > 0)
            {
                var existingPending = existingPendingRequests.First();
                var currentPendingSteps = (await _stepRepo.FindAsync(s =>
                    s.ApprovalRequestId == existingPending.Id &&
                    s.Level == existingPending.CurrentLevel &&
                    s.Status == ApprovalStatus.Pending, ct)).ToList();

                if (currentPendingSteps.Any(s => s.ApproverAccountId == expectedCurrentApproverId))
                {
                    await ArchiveStalePendingWorkflowsAsync(existingPendingRequests.Skip(1), ct);
                    return existingPending.Id;
                }

                await ArchiveStalePendingWorkflowsAsync(existingPendingRequests, ct);
            }

            var newRequestId = 0;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var request = new ApprovalRequest
                {
                    ModuleCode = moduleCode,
                    ReferenceId = referenceId,
                    CurrentLevel = 1,
                    Status = ApprovalStatus.Pending
                };

                await _requestRepo.AddAsync(request, ct);
                await _unitOfWork.CommitAsync(ct);

                var level = 1;
                foreach (var approverId in approverAccountIds)
                {
                    await _stepRepo.AddAsync(new ApprovalStep
                    {
                        ApprovalRequestId = request.Id,
                        Level = level++,
                        ApproverAccountId = approverId,
                        Status = ApprovalStatus.Pending
                    }, ct);
                }

                await _unitOfWork.CommitAsync(ct);
                newRequestId = request.Id;
            }, ct);

            return newRequestId;
        }

        private async Task ArchiveStalePendingWorkflowsAsync(
            IEnumerable<ApprovalRequest> requests,
            CancellationToken ct)
        {
            var staleRequests = requests.ToList();
            if (staleRequests.Count == 0)
                return;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                foreach (var request in staleRequests)
                {
                    request.Status = ApprovalStatus.NeedMoreInfo;
                    await _requestRepo.UpdateAsync(request, ct);

                    var pendingSteps = (await _stepRepo.FindAsync(s =>
                        s.ApprovalRequestId == request.Id &&
                        s.Status == ApprovalStatus.Pending, ct)).ToList();

                    foreach (var step in pendingSteps)
                    {
                        step.Status = ApprovalStatus.NeedMoreInfo;
                        step.Note = "Workflow pending khong con khop voi buoc nghiep vu hien tai.";
                        step.ProcessedAt = DateTime.UtcNow;
                        await _stepRepo.UpdateAsync(step, ct);
                    }
                }

                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            bool isApproved,
            string? note = null,
            CancellationToken ct = default)
        {
            return await ProcessStepAsync(
                moduleCode,
                referenceId,
                approverAccountId,
                actorRoleName,
                isApproved ? ApprovalWorkflowAction.Approve : ApprovalWorkflowAction.Reject,
                note,
                ct);
        }

        public async Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            ApprovalWorkflowAction action,
            string? note = null,
            CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync(
                LockKeys.Approval(moduleCode, referenceId),
                innerCt => ProcessStepCoreAsync(moduleCode, referenceId, approverAccountId, actorRoleName, action, note, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ApprovalStatus> ProcessStepCoreAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            ApprovalWorkflowAction action,
            string? note = null,
            CancellationToken ct = default)
        {
            var finalWorkflowStatus = ApprovalStatus.Pending;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var request = (await _requestRepo.FindAsync(r =>
                    r.ModuleCode == moduleCode &&
                    r.ReferenceId == referenceId &&
                    r.Status == ApprovalStatus.Pending, ct))
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (request == null)
                    throw new InvalidOperationException("Yêu cầu duyệt không tồn tại hoặc đã được xử lý.");

                var currentStep = (await _stepRepo.FindAsync(s =>
                    s.ApprovalRequestId == request.Id &&
                    s.Level == request.CurrentLevel, ct)).FirstOrDefault();

                if (currentStep == null)
                    throw new InvalidOperationException("Không tìm thấy bước duyệt hiện tại.");

                var isYourStep =
                    currentStep.ApproverAccountId == approverAccountId ||
                    (!string.IsNullOrEmpty(currentStep.ApproverRoleName) &&
                     currentStep.ApproverRoleName == actorRoleName);

                if (!isYourStep &&
                    moduleCode == "CANDIDATE" &&
                    request.CurrentLevel == 1 &&
                    string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    isYourStep = await IsCandidateDepartmentManagerAsync(referenceId, approverAccountId, ct);
                }

                if (!isYourStep &&
                    moduleCode == "CONTRACT_DEPT" &&
                    request.CurrentLevel == 1 &&
                    string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    isYourStep = await IsContractDepartmentManagerAsync(referenceId, approverAccountId, ct);
                }

                if (!isYourStep && CanRoleProcessStep(moduleCode, request.CurrentLevel, actorRoleName))
                {
                    isYourStep = true;
                }

                if (!isYourStep)
                    throw new UnauthorizedAccessException("Bạn không có quyền duyệt ở bước này.");

                currentStep.Status = ResolveStepStatus(action);
                currentStep.Note = note;
                currentStep.ProcessedAt = DateTime.UtcNow;
                currentStep.ApproverAccountId = approverAccountId;
                await _stepRepo.UpdateAsync(currentStep, ct);

                if (action == ApprovalWorkflowAction.RequestRevision)
                {
                    request.Status = ApprovalStatus.NeedMoreInfo;
                }
                else if (action == ApprovalWorkflowAction.Reject)
                {
                    request.Status = ApprovalStatus.Rejected;
                }
                else
                {
                    var nextStepExists = (await _stepRepo.FindAsync(s =>
                        s.ApprovalRequestId == request.Id &&
                        s.Level == request.CurrentLevel + 1, ct)).Any();

                    if (nextStepExists)
                    {
                        request.CurrentLevel++;
                        await _mediator.Publish(new ApprovalLevelChangedEvent
                        {
                            ModuleCode = request.ModuleCode,
                            ReferenceId = request.ReferenceId,
                            NewLevel = request.CurrentLevel
                        }, ct);
                    }
                    else
                    {
                        request.Status = ApprovalStatus.Approved;
                    }
                }

                await _requestRepo.UpdateAsync(request, ct);

                if (request.Status != ApprovalStatus.Pending)
                {
                    await _mediator.Publish(new ApprovalCompletedEvent
                    {
                        ModuleCode = request.ModuleCode,
                        ReferenceId = request.ReferenceId,
                        FinalStatus = request.Status,
                        Action = action,
                        Note = note
                    }, ct);
                }

                await _unitOfWork.CommitAsync(ct);
                finalWorkflowStatus = request.Status;
            }, ct);

            return finalWorkflowStatus;
        }

        public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(
            int approverId,
            string actorRoleName,
            CancellationToken ct = default)
        {
            var pendingSteps = (await _stepRepo.FindAsync(s =>
                s.Status == ApprovalStatus.Pending &&
                (s.ApproverAccountId == approverId || s.ApproverRoleName == actorRoleName), ct)).ToList();

            if (string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                var candidateApprovalRequests = (await _requestRepo.FindAsync(r =>
                    r.ModuleCode == "CANDIDATE" &&
                    r.Status == ApprovalStatus.Pending &&
                    r.CurrentLevel == 1, ct)).ToList();

                if (candidateApprovalRequests.Any())
                {
                    var managerFallbackCandidateIds = candidateApprovalRequests.Select(r => r.ReferenceId).Distinct().ToList();
                    var managerFallbackCandidates = await _candidateRepo.GetCandidatesWithDetailsAsync(managerFallbackCandidateIds, ct);
                    var managedCandidateIds = managerFallbackCandidates
                        .Where(c => c.RecruitmentRequest?.Department?.Manager?.AccountId == approverId)
                        .Select(c => c.Id)
                        .ToHashSet();

                    var managedApprovalRequestIds = candidateApprovalRequests
                        .Where(r => managedCandidateIds.Contains(r.ReferenceId))
                        .Select(r => r.Id)
                        .ToHashSet();

                    if (managedApprovalRequestIds.Any())
                    {
                        var managedSteps = await _stepRepo.FindAsync(s =>
                            managedApprovalRequestIds.Contains(s.ApprovalRequestId) &&
                            s.Status == ApprovalStatus.Pending &&
                            s.Level == 1, ct);

                        foreach (var step in managedSteps)
                        {
                            if (pendingSteps.All(s => s.Id != step.Id))
                                pendingSteps.Add(step);
                        }
                    }
                }

                var contractApprovalRequests = (await _requestRepo.FindAsync(r =>
                    r.ModuleCode == "CONTRACT_DEPT" &&
                    r.Status == ApprovalStatus.Pending &&
                    r.CurrentLevel == 1, ct)).ToList();

                if (contractApprovalRequests.Any())
                {
                    var managerFallbackContractIds = contractApprovalRequests.Select(r => r.ReferenceId).Distinct().ToList();
                    var managerFallbackContracts = await _contractRepo.GetContractsWithDetailsAsync(managerFallbackContractIds, ct);
                    var managedContractIds = managerFallbackContracts
                        .Where(c => c.Employee?.Department?.Manager?.AccountId == approverId)
                        .Select(c => c.Id)
                        .ToHashSet();

                    var managedApprovalRequestIds = contractApprovalRequests
                        .Where(r => managedContractIds.Contains(r.ReferenceId))
                        .Select(r => r.Id)
                        .ToHashSet();

                    if (managedApprovalRequestIds.Any())
                    {
                        var managedSteps = await _stepRepo.FindAsync(s =>
                            managedApprovalRequestIds.Contains(s.ApprovalRequestId) &&
                            s.Status == ApprovalStatus.Pending &&
                            s.Level == 1, ct);

                        foreach (var step in managedSteps)
                        {
                            if (pendingSteps.All(s => s.Id != step.Id))
                                pendingSteps.Add(step);
                        }
                    }
                }
            }

            var roleFallbackRequests = (await _requestRepo.FindAsync(r =>
                r.Status == ApprovalStatus.Pending, ct))
                .Where(r => CanRoleProcessStep(r.ModuleCode, r.CurrentLevel, actorRoleName))
                .ToList();

            if (roleFallbackRequests.Any())
            {
                var roleFallbackRequestIds = roleFallbackRequests.Select(r => r.Id).ToHashSet();
                var roleFallbackSteps = await _stepRepo.FindAsync(s =>
                    roleFallbackRequestIds.Contains(s.ApprovalRequestId) &&
                    s.Status == ApprovalStatus.Pending, ct);

                foreach (var step in roleFallbackSteps)
                {
                    if (pendingSteps.All(s => s.Id != step.Id))
                        pendingSteps.Add(step);
                }
            }

            if (!pendingSteps.Any()) return Enumerable.Empty<PendingApprovalDto>();

            var requestIds = pendingSteps.Select(s => s.ApprovalRequestId).Distinct().ToList();
            var requests = await _requestRepo.FindAsync(r =>
                requestIds.Contains(r.Id) &&
                r.Status == ApprovalStatus.Pending, ct);

            var validRequests = requests.ToDictionary(r => r.Id);
            var activeSteps = pendingSteps.Where(s =>
                validRequests.TryGetValue(s.ApprovalRequestId, out var request) &&
                request.CurrentLevel == s.Level);

            var result = new List<PendingApprovalDto>();
            var activeRequests = validRequests.Values.ToList();
            var recruitmentIds = activeRequests
                .Where(r => r.ModuleCode == "RECRUITMENT")
                .Select(r => r.ReferenceId)
                .ToList();
            var candidateIds = activeRequests
                .Where(r => r.ModuleCode == "CANDIDATE")
                .Select(r => r.ReferenceId)
                .ToList();
            var contractIds = activeRequests
                .Where(r => r.ModuleCode is "CONTRACT_DEPT" or "CONTRACT_DIRECTOR")
                .Select(r => r.ReferenceId)
                .ToList();

            var recruitmentRequests = recruitmentIds.Any()
                ? await _reqRepo.GetRequestsWithDetailsAsync(recruitmentIds, ct)
                : new List<RecruitmentRequest>();
            var candidates = candidateIds.Any()
                ? await _candidateRepo.GetCandidatesWithDetailsAsync(candidateIds, ct)
                : new List<Candidate>();
            var contracts = contractIds.Any()
                ? await _contractRepo.GetContractsWithDetailsAsync(contractIds, ct)
                : new List<Core.Entities.EmployeeProfile.Contract>();

            foreach (var step in activeSteps)
            {
                var req = validRequests[step.ApprovalRequestId];
                var dto = BuildBaseDto(req, step);

                if (req.ModuleCode == "RECRUITMENT")
                {
                    var rec = recruitmentRequests.FirstOrDefault(r => r.Id == req.ReferenceId);
                    ApplyRecruitmentDetails(dto, rec);
                }
                else if (req.ModuleCode == "CANDIDATE")
                {
                    var candidate = candidates.FirstOrDefault(c => c.Id == req.ReferenceId);
                    ApplyCandidateDetails(dto, candidate);
                }
                else if (req.ModuleCode is "CONTRACT_DEPT" or "CONTRACT_DIRECTOR")
                {
                    var contract = contracts.FirstOrDefault(c => c.Id == req.ReferenceId);
                    ApplyContractDetails(dto, contract);
                }
                else
                {
                    ApplyGenericDetails(dto);
                }

                dto.Actions = BuildActions(req.ModuleCode, dto.DetailRoute);
                result.Add(dto);
            }

            return result;
        }

        private async Task<bool> IsCandidateDepartmentManagerAsync(
            int candidateId,
            int approverAccountId,
            CancellationToken ct)
        {
            var candidate = (await _candidateRepo.GetCandidatesWithDetailsAsync(new List<int> { candidateId }, ct))
                .FirstOrDefault();

            return candidate?.RecruitmentRequest?.Department?.Manager?.AccountId == approverAccountId;
        }

        private async Task<bool> IsContractDepartmentManagerAsync(
            int contractId,
            int approverAccountId,
            CancellationToken ct)
        {
            var contract = await _contractRepo.GetByIdAsync(contractId, ct);
            return contract?.Employee?.Department?.Manager?.AccountId == approverAccountId;
        }

        private static bool CanRoleProcessStep(string moduleCode, int level, string actorRoleName)
        {
            if (string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            return moduleCode switch
            {
                "RECRUITMENT" when level == 1 => string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase),
                "RECRUITMENT" when level == 2 => string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase),
                "CANDIDATE" when level == 2 => string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase),
                "CONTRACT_DIRECTOR" when level == 1 => string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static ApprovalStatus ResolveStepStatus(ApprovalWorkflowAction action)
        {
            return action switch
            {
                ApprovalWorkflowAction.Approve => ApprovalStatus.Approved,
                ApprovalWorkflowAction.RequestRevision => ApprovalStatus.NeedMoreInfo,
                _ => ApprovalStatus.Rejected
            };
        }

        private static PendingApprovalDto BuildBaseDto(ApprovalRequest request, ApprovalStep step)
        {
            return new PendingApprovalDto
            {
                ApprovalRequestId = request.Id,
                ModuleCode = request.ModuleCode,
                ReferenceId = request.ReferenceId,
                Level = step.Level,
                CreatedAt = request.CreatedAt,
                Status = request.Status.ToString(),
                StatusLabel = "Chờ duyệt cấp " + step.Level,
                Title = GetDefaultTitle(request.ModuleCode, request.ReferenceId),
                DetailRoute = ResolveDetailRoute(request.ModuleCode, request.ReferenceId),
                DetailTitle = GetDefaultTitle(request.ModuleCode, request.ReferenceId)
            };
        }

        private static void ApplyRecruitmentDetails(PendingApprovalDto dto, RecruitmentRequest? request)
        {
            if (request == null)
            {
                ApplyGenericDetails(dto);
                return;
            }

            dto.Title = request.Position?.Title ?? "Nhu cầu tuyển dụng";
            dto.Quantity = request.Quantity;
            dto.Deadline = request.Deadline;
            dto.Description = request.Description;
            dto.DepartmentName = request.Department?.DeptName;
            dto.PositionName = request.Position?.Title;
            dto.DetailTitle = "Nhu cầu tuyển dụng";
            dto.DetailRoute = "/recruitment/demands";
            dto.DetailFields = VisibleFields(
                ("Mã tham chiếu", "#" + dto.ReferenceId),
                ("Cấp duyệt", dto.Level.ToString()),
                ("Vị trí", dto.PositionName),
                ("Phòng ban", dto.DepartmentName),
                ("Số lượng", dto.Quantity.HasValue ? dto.Quantity + " nhân sự" : null),
                ("Hạn tuyển", FormatDate(dto.Deadline)),
                ("Mô tả", dto.Description));
        }

        private static void ApplyCandidateDetails(PendingApprovalDto dto, Candidate? candidate)
        {
            if (candidate == null)
            {
                ApplyGenericDetails(dto);
                return;
            }

            dto.Title = candidate.FullName;
            dto.CvFilePath = candidate.CvFilePath;
            dto.PositionName = candidate.RecruitmentRequest?.Position?.Title;
            dto.DepartmentName = candidate.RecruitmentRequest?.Department?.DeptName;
            dto.DetailTitle = "Ứng viên";
            dto.DetailRoute = "/recruitment/candidates";
            dto.DetailFields = VisibleFields(
                ("Mã tham chiếu", "#" + dto.ReferenceId),
                ("Cấp duyệt", dto.Level.ToString()),
                ("Ứng viên", candidate.FullName),
                ("Vị trí", dto.PositionName),
                ("Phòng ban", dto.DepartmentName),
                ("CV", dto.CvFilePath));
        }

        private static void ApplyContractDetails(
            PendingApprovalDto dto,
            Core.Entities.EmployeeProfile.Contract? contract)
        {
            if (contract == null)
            {
                ApplyGenericDetails(dto);
                return;
            }

            dto.Title = "Hợp đồng: " + (contract.Employee?.FullName ?? "#" + contract.EmployeeId);
            dto.Description = contract.ContractType.ToString();
            dto.DepartmentName = contract.Employee?.Department?.DeptName;
            dto.PositionName = contract.Employee?.Position?.Title;
            dto.DetailTitle = "Hợp đồng";
            dto.DetailRoute = "/employee-contract/contracts";
            dto.DetailFields = VisibleFields(
                ("Mã tham chiếu", "#" + dto.ReferenceId),
                ("Cấp duyệt", dto.Level.ToString()),
                ("Nhân sự", contract.Employee?.FullName),
                ("Số hợp đồng", contract.ContractNumber),
                ("Loại hợp đồng", contract.ContractType.ToString()),
                ("Phòng ban", dto.DepartmentName),
                ("Chức danh", dto.PositionName));
        }

        private static void ApplyGenericDetails(PendingApprovalDto dto)
        {
            dto.DetailFields = VisibleFields(
                ("Mã tham chiếu", "#" + dto.ReferenceId),
                ("Cấp duyệt", dto.Level.ToString()),
                ("Phân hệ", dto.ModuleCode),
                ("Mô tả", dto.Description));
        }

        private static List<PendingApprovalActionDto> BuildActions(string moduleCode, string? detailRoute)
        {
            var actions = new List<PendingApprovalActionDto>
            {
                new()
                {
                    Kind = "approve",
                    Label = "Duyệt",
                    Tone = "primary",
                    RequiresNote = false,
                    Endpoint = "/api/v1/approvals/process"
                },
                new()
                {
                    Kind = "reject",
                    Label = "Từ chối",
                    Tone = "danger",
                    RequiresNote = true,
                    Endpoint = "/api/v1/approvals/process"
                },
                new()
                {
                    Kind = "revision",
                    Label = "Yêu cầu bổ sung",
                    Tone = "secondary",
                    RequiresNote = true,
                    Endpoint = "/api/v1/approvals/process"
                }
            };

            if (!string.IsNullOrWhiteSpace(detailRoute))
            {
                actions.Add(new PendingApprovalActionDto
                {
                    Kind = "open",
                    Label = "Xem chi tiết",
                    Tone = "secondary",
                    RequiresNote = false,
                    Endpoint = detailRoute,
                    Method = "GET"
                });
            }

            return actions;
        }

        private static string GetDefaultTitle(string moduleCode, int referenceId)
        {
            return moduleCode switch
            {
                "RECRUITMENT" => "Nhu cầu tuyển dụng",
                "CANDIDATE" => "Ứng viên",
                "CONTRACT_DEPT" or "CONTRACT_DIRECTOR" => "Hợp đồng",
                _ => moduleCode + " #" + referenceId
            };
        }

        private static string? ResolveDetailRoute(string moduleCode, int referenceId)
        {
            return moduleCode switch
            {
                "RECRUITMENT" => "/recruitment/demands",
                "CANDIDATE" => "/recruitment/candidates",
                "CONTRACT_DEPT" or "CONTRACT_DIRECTOR" => "/employee-contract/contracts",
                _ => null
            };
        }

        private static List<PendingApprovalDetailFieldDto> VisibleFields(
            params (string Label, string? Value)[] fields)
        {
            return fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Value))
                .Select(field => new PendingApprovalDetailFieldDto
                {
                    Label = field.Label,
                    Value = field.Value
                })
                .ToList();
        }

        private static string? FormatDate(DateTime? value)
        {
            return value?.ToString("dd/MM/yyyy");
        }
    }
}
