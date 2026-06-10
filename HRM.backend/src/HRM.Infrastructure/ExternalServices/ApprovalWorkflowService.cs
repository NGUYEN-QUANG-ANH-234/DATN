
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using MediatR;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;

using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IBaseRepository<ApprovalRequest> _requestRepo;
        private readonly IBaseRepository<ApprovalStep> _stepRepo;
        private readonly IRecruitmentRequestRepository _reqRepo;
        private readonly ICandidateRepository _candidateRepo;
        private readonly IContractRepository _contractRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public ApprovalWorkflowService(
            IBaseRepository<ApprovalRequest> requestRepo,
            IBaseRepository<ApprovalStep> stepRepo,
            IRecruitmentRequestRepository reqRepo,
            ICandidateRepository candidateRepo,
            IContractRepository contractRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _requestRepo = requestRepo;
            _stepRepo = stepRepo;
            _reqRepo = reqRepo;
            _candidateRepo = candidateRepo;
            _contractRepo = contractRepo;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<int> CreateWorkflowAsync(string moduleCode, int referenceId, List<int> approverAccountIds, CancellationToken ct = default)
        {
            if (approverAccountIds == null || !approverAccountIds.Any())
                throw new ArgumentException("Phải có ít nhất 1 người duyệt.");

            int newRequestId = 0;

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
                await _unitOfWork.CommitAsync(ct); // Lấy request.Id thực tế

                int level = 1;
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

        public async Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            bool isApproved,
            string? note = null,
            CancellationToken ct = default)
        {
            var finalWorkflowStatus = ApprovalStatus.Pending;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // 1. Tìm đúng đơn duyệt trung tâm bằng cặp trùng khớp
                var request = (await _requestRepo.FindAsync(r =>
                    r.ModuleCode == moduleCode &&
                    r.ReferenceId == referenceId &&
                    r.Status == ApprovalStatus.Pending, ct)).FirstOrDefault();

                if (request == null)
                    throw new InvalidOperationException("Yêu cầu duyệt không tồn tại hoặc đã được đóng.");

                // 2. Tìm đúng bước duyệt hiện tại của đơn tổng thể này
                var currentStep = (await _stepRepo.FindAsync(s =>
                    s.ApprovalRequestId == request.Id &&
                    s.Level == request.CurrentLevel, ct)).FirstOrDefault();

                if (currentStep == null)
                    throw new InvalidOperationException("Không tìm thấy bước duyệt hiện tại.");

                // 3. Kiểm tra quyền chính xác (Đích danh Id HOẶC khớp Nhóm quyền Role)
                bool isYourStep = (currentStep.ApproverAccountId.HasValue && currentStep.ApproverAccountId.Value == approverAccountId) ||
                                  (!string.IsNullOrEmpty(currentStep.ApproverRoleName) && currentStep.ApproverRoleName == actorRoleName);

                if (!isYourStep)
                    throw new UnauthorizedAccessException("Bạn không có quyền duyệt ở cấp độ này.");

                // 4. Cập nhật thông tin bước duyệt hiện tại
                currentStep.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
                currentStep.Note = note;
                currentStep.ProcessedAt = DateTime.UtcNow;
                currentStep.ApproverAccountId = approverAccountId; // Lưu đích danh người bấm nút
                await _stepRepo.UpdateAsync(currentStep, ct);

                // 5. Tính toán bước đi tiếp theo cho luồng duyệt
                if (!isApproved)
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

                        // QUAN TRỌNG: Bắn event thông báo chuyển cấp độ (Ví dụ: Từ cấp 1 lên cấp 2)
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

                // 6. Nếu quy trình kết thúc (Approved/Rejected) -> Bắn Event hoàn thành tổng thể
                if (request.Status != ApprovalStatus.Pending)
                {
                    await _mediator.Publish(new ApprovalCompletedEvent
                    {
                        ModuleCode = request.ModuleCode,
                        ReferenceId = request.ReferenceId,
                        FinalStatus = request.Status,
                        Note = note
                    }, ct);
                }

                await _unitOfWork.CommitAsync(ct);
                finalWorkflowStatus = request.Status;
            }, ct);

            return finalWorkflowStatus;
        }

        public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(int approverId, string actorRoleName, CancellationToken ct = default)
        {
            var pendingSteps = await _stepRepo.FindAsync(s => 
                s.Status == ApprovalStatus.Pending && 
                (s.ApproverAccountId == approverId || s.ApproverRoleName == actorRoleName), ct);

            if (!pendingSteps.Any()) return Enumerable.Empty<PendingApprovalDto>();

            var requestIds = pendingSteps.Select(s => s.ApprovalRequestId).Distinct();
            var requests = await _requestRepo.FindAsync(r => requestIds.Contains(r.Id) && r.Status == ApprovalStatus.Pending, ct);

            // Filter out steps that are not for the current level
            var validRequests = requests.ToDictionary(r => r.Id);
            var activeSteps = pendingSteps.Where(s => validRequests.ContainsKey(s.ApprovalRequestId) && validRequests[s.ApprovalRequestId].CurrentLevel == s.Level);

            var result = new List<PendingApprovalDto>();

            // Fetch referenced entities
            var recruitmentIds = validRequests.Values.Where(r => r.ModuleCode == "RECRUITMENT").Select(r => r.ReferenceId).ToList();
            var candidateIds = validRequests.Values.Where(r => r.ModuleCode == "CANDIDATE").Select(r => r.ReferenceId).ToList();

            var recruitmentRequests = recruitmentIds.Any() ? await _reqRepo.GetRequestsWithDetailsAsync(recruitmentIds, ct) : new List<RecruitmentRequest>();
            var candidates = candidateIds.Any() ? await _candidateRepo.GetCandidatesWithDetailsAsync(candidateIds, ct) : new List<Candidate>();
            
            var contractIds = validRequests.Values.Where(r => r.ModuleCode == "CONTRACT_DEPT" || r.ModuleCode == "CONTRACT_DIRECTOR").Select(r => r.ReferenceId).ToList();
            var contracts = contractIds.Any() ? await _contractRepo.GetContractsWithDetailsAsync(contractIds, ct) : new List<Core.Entities.EmployeeProfile.Contract>();

            // If navigation properties Department/Position are not eagerly loaded by the base repo, we might need a separate query or assume they are included.
            // For safety, let's load them manually if needed, or rely on lazy loading/includes if configured.

            foreach (var step in activeSteps)
            {
                var req = validRequests[step.ApprovalRequestId];
                var dto = new PendingApprovalDto
                {
                    ApprovalRequestId = req.Id,
                    ModuleCode = req.ModuleCode,
                    ReferenceId = req.ReferenceId,
                    Level = step.Level,
                    CreatedAt = req.CreatedAt // Wait, ApprovalRequest doesn't have CreatedAt? Let's check...
                };

                if (req.ModuleCode == "RECRUITMENT")
                {
                    var rec = recruitmentRequests.FirstOrDefault(r => r.Id == req.ReferenceId);
                    if (rec != null)
                    {
                        dto.Title = rec.Position?.Title ?? "Yêu cầu tuyển dụng";
                        dto.Quantity = rec.Quantity;
                        dto.Deadline = rec.Deadline;
                        dto.Description = rec.Description;
                        dto.DepartmentName = rec.Department?.DeptName;
                        dto.PositionName = rec.Position?.Title;
                    }
                }
                else if (req.ModuleCode == "CANDIDATE")
                {
                    var cand = candidates.FirstOrDefault(c => c.Id == req.ReferenceId);
                    if (cand != null)
                    {
                        dto.Title = cand.FullName;
                        dto.CvFilePath = cand.CvFilePath;
                        dto.PositionName = cand.RecruitmentRequest?.Position?.Title;
                        dto.DepartmentName = cand.RecruitmentRequest?.Department?.DeptName;
                    }
                }
                else if (req.ModuleCode == "CONTRACT_DEPT" || req.ModuleCode == "CONTRACT_DIRECTOR")
                {
                    var contract = contracts.FirstOrDefault(c => c.Id == req.ReferenceId);
                    if (contract != null)
                    {
                        dto.Title = $"Y/C ký kết/gia hạn: {contract.Employee?.FullName}";
                        dto.Description = contract.ContractType.ToString();
                        dto.DepartmentName = contract.Employee?.Department?.DeptName;
                        dto.PositionName = contract.Employee?.Position?.Title;
                    }
                }

                result.Add(dto);
            }

            return result;
        }
    }
}
