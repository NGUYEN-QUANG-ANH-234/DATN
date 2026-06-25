using HRM.backend.src.HRM.Application.DTOs.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using System.Net;

namespace HRM.backend.src.HRM.Application.UseCases.Recruitment
{
    public class CandidateUseCase : ICandidateUseCase
    {
        private readonly ICandidateRepository _candidateRepo;
        private readonly IRecruitmentRequestRepository _reqRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IStorageService _storageService;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly ISlaTrackingRepository _slaTrackingRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IApprovalWorkflowService _approvalService;
        private readonly IAccountRepository _accountRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;

        public CandidateUseCase(
            ICandidateRepository candidateRepo,
            IRecruitmentRequestRepository reqRepo,
            IEmployeeRepository employeeRepo,
            IStorageService storageService,
            ISlaTrackingService slaTrackingService,
            ISlaTrackingRepository slaTrackingRepo,
            IAuditLogRepository auditLogRepo,
            IApprovalWorkflowService approvalService,
            IAccountRepository accountRepo,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IIdempotencyService idempotencyService)
        {
            _candidateRepo = candidateRepo;
            _reqRepo = reqRepo;
            _employeeRepo = employeeRepo;
            _storageService = storageService;
            _slaTrackingService = slaTrackingService;
            _slaTrackingRepo = slaTrackingRepo;
            _auditLogRepo = auditLogRepo;
            _approvalService = approvalService;
            _accountRepo = accountRepo;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
        }

        public async Task<ApplyJobResultDto> ApplyForJobAsync(ApplyJobDto dto, CancellationToken ct = default, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("CANDIDATE_APPLY", idempotencyKey, ct);
            if (existingResourceId.HasValue)
            {
                var existing = await _candidateRepo.GetByIdAsync(existingResourceId.Value, ct);
                if (existing != null)
                    return new ApplyJobResultDto { CandidateId = existing.Id, TrackingCode = existing.TrackingCode ?? string.Empty };
            }

            // 1. Kiểm tra nghiệp vụ: Tin tuyển dụng
            var job = await _reqRepo.GetByIdWithCandidatesAsync(dto.RecruitmentRequestId, ct);
            if (job == null)
                throw new InvalidOperationException("Tin tuyển dụng không tồn tại.");

            if (job.Status != RecruitmentRequestStatus.Approved)
                throw new InvalidOperationException("Tin tuyển dụng này chưa được mở hoặc đã đóng.");

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Tin tuyển dụng này đã hết hạn nộp hồ sơ.");

            // 2. Tìm kiếm ứng viên theo Email và Job
            await EnsureJobCanReceiveApplicationsAsync(job, ct);

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            return await _lockService.GetWithLockAsync($"candidate_apply_{dto.RecruitmentRequestId}_{normalizedEmail}", async (innerCt) =>
            {
            var currentJob = await _reqRepo.GetByIdWithCandidatesAsync(dto.RecruitmentRequestId, innerCt);
            if (currentJob == null)
                throw new InvalidOperationException("Tin tuyển dụng không tồn tại.");

            await EnsureJobCanReceiveApplicationsAsync(currentJob, innerCt);

            var existingCandidate = (await _candidateRepo.FindAsync(c =>
                c.RecruitmentRequestId == dto.RecruitmentRequestId &&
                c.Email != null && c.Email.ToLower() == normalizedEmail, innerCt)).FirstOrDefault();

            // 3. Upload CV mới
            string newCvUrl = await _storageService.UploadFileAsync(dto.CvFile, "cvs", innerCt);

            string generatedTrackingCode = GenerateTrackingCode();

            if (existingCandidate != null)
            {
                // Kịch bản ứng viên nộp lại (Ghi đè)
                if (existingCandidate.Status != CandidateStatus.New)
                {
                    throw new InvalidOperationException("Hồ sơ của bạn đang được bộ phận nhân sự xử lý, không thể thay thế CV lúc này.");
                }

                // Cập nhật thông tin và file mới
                existingCandidate.CvFilePath = newCvUrl;
                existingCandidate.FullName = dto.FullName;
                existingCandidate.AppliedDate = DateTime.UtcNow.Date;
                existingCandidate.TrackingCode = generatedTrackingCode;

                await _candidateRepo.UpdateAsync(existingCandidate, innerCt);
                await SendApplicationReceiptEmailAsync(existingCandidate, currentJob, generatedTrackingCode);
                await _unitOfWork.CommitAsync(innerCt);
                await _idempotencyService.SaveAsync("CANDIDATE_APPLY", idempotencyKey ?? string.Empty, "Candidate", existingCandidate.Id, null, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                return new ApplyJobResultDto { CandidateId = existingCandidate.Id, TrackingCode = generatedTrackingCode };
            }
            else
            {
                // Kịch bản nộp mới tinh
                var newCandidate = new Candidate
                {
                    RecruitmentRequestId = dto.RecruitmentRequestId,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    TrackingCode = generatedTrackingCode,
                    CvFilePath = newCvUrl,
                    Status = CandidateStatus.New,
                    AppliedDate = DateTime.UtcNow.Date
                };

                await _candidateRepo.AddAsync(newCandidate, innerCt);
                await SendApplicationReceiptEmailAsync(newCandidate, currentJob, generatedTrackingCode);
                await _unitOfWork.CommitAsync(innerCt);
                await _idempotencyService.SaveAsync("CANDIDATE_APPLY", idempotencyKey ?? string.Empty, "Candidate", newCandidate.Id, null, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                return new ApplyJobResultDto { CandidateId = newCandidate.Id, TrackingCode = generatedTrackingCode };
            }
            }, cancellationToken: ct);
        }

        public async Task<IEnumerable<CandidateHistoryDto>> GetMyApplicationsAsync(string email, string? trackingCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return Enumerable.Empty<CandidateHistoryDto>();

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedTrackingCode = trackingCode?.Trim().ToUpperInvariant();

            var candidates = await _candidateRepo.FindAsync(c => c.Email != null && c.Email.ToLower() == normalizedEmail &&
                (string.IsNullOrEmpty(normalizedTrackingCode) ||
                    (c.TrackingCode != null && c.TrackingCode.ToUpper() == normalizedTrackingCode)), ct);
            var reqIds = candidates.Where(c => c.RecruitmentRequestId.HasValue).Select(c => c.RecruitmentRequestId!.Value).Distinct();
            var requests = await _reqRepo.FindAsync(r => reqIds.Contains(r.Id), ct); // Need Department Name? The Repo usually doesn't include it unless Include is used. For now just return JobTitle (PositionName usually in Position? Wait, we need to check RecruitmentRequest entity)

            var history = new List<CandidateHistoryDto>();
            foreach (var c in candidates)
            {
                var req = requests.FirstOrDefault(r => r.Id == c.RecruitmentRequestId);
                // We mock DepartmentName and JobTitle if Navigation properties are not loaded.
                // Ideally _reqRepo should include Dept and Position.
                history.Add(new CandidateHistoryDto
                {
                    CandidateId = c.Id,
                    RecruitmentRequestId = c.RecruitmentRequestId ?? 0,
                    FullName = c.FullName,
                    CvFilePath = c.CvFilePath,
                    Email = c.Email!,
                    JobTitle = req != null && req.Position != null ? req.Position.Title : (req?.Description ?? "Unknown Position"),
                    DepartmentName = req != null && req.Department != null ? req.Department.DeptName : "Unknown Department",
                    Status = c.Status.ToString(),
                    AppliedDate = c.AppliedDate
                });
            }

            return history.OrderByDescending(h => h.AppliedDate);
        }

        public async Task<IEnumerable<CandidateHistoryDto>> GetAllCandidatesAsync(int userId, string actorRoleName, CancellationToken ct = default)
        {
            var candidates = await _candidateRepo.GetAllAsync(ct);
            var requests = await _reqRepo.GetAllAsync(ct);

            // Phân quyền: Manager chỉ xem CV nộp vào phòng ban của mình
            if (IsManager(actorRoleName))
            {
                var managerDeptId = await GetManagerDeptIdAsync(userId, ct);

                var allowedReqIds = requests.Where(r => r.DeptId == managerDeptId).Select(r => r.Id).ToList();
                candidates = candidates.Where(c => c.RecruitmentRequestId.HasValue && allowedReqIds.Contains(c.RecruitmentRequestId.Value));
            }

            var result = new List<CandidateHistoryDto>();
            foreach (var c in candidates)
            {
                var req = requests.FirstOrDefault(r => r.Id == c.RecruitmentRequestId);
                result.Add(new CandidateHistoryDto
                {
                    CandidateId = c.Id,
                    RecruitmentRequestId = c.RecruitmentRequestId ?? 0,
                    FullName = c.FullName,
                    CvFilePath = c.CvFilePath,
                    Email = c.Email!,
                    JobTitle = req != null && req.Position != null ? req.Position.Title : (req?.Description ?? "Unknown Position"),
                    DepartmentName = req != null && req.Department != null ? req.Department.DeptName : "Unknown Department",
                    Status = c.Status.ToString(),
                    AppliedDate = c.AppliedDate
                });
            }

            return result.OrderByDescending(x => x.AppliedDate);
        }

        public async Task<bool> HrApproveAsync(int candidateId, int actorId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"candidate_{candidateId}", async (innerCt) =>
            {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            if (candidate.Status != CandidateStatus.New) throw new InvalidOperationException("Hồ sơ đã được xử lý trước đó.");

            var request = await _reqRepo.GetByIdWithCandidatesAsync(candidate.RecruitmentRequestId ?? 0, ct);
            if (request == null || !request.DeptId.HasValue) throw new InvalidOperationException("Yêu cầu tuyển dụng không xác định được phòng ban.");
            await EnsureManagerCanAccessRequestAsync(request, actorId, actorRoleName, ct);

            var configuredManagerEmployee = request.Department?.Manager;
            var managerAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Manager", ct);
            var managerEmployee = (await _employeeRepo.FindAsync(e => e.DeptId == request.DeptId && e.AccountId.HasValue && managerAccountIds.Contains(e.AccountId.Value), ct)).FirstOrDefault();
            managerEmployee = configuredManagerEmployee?.AccountId.HasValue == true ? configuredManagerEmployee : managerEmployee;
            if (managerEmployee == null) throw new InvalidOperationException("Không tìm thấy Trưởng phòng của phòng ban này.");

            var directorAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
            int directorId = directorAccountIds.FirstOrDefault();
            if (directorId == 0) throw new InvalidOperationException("Không tìm thấy Giám đốc trong hệ thống.");

            candidate.Status = CandidateStatus.Interview_Pending;
            await _candidateRepo.UpdateAsync(candidate, ct);

             // Tích hợp Workflow Duyệt
            await _approvalService.CreateWorkflowAsync("CANDIDATE", candidate.Id, new List<int> { managerEmployee.AccountId!.Value, directorId }, ct);

            // Bắt đầu tính SLA 15 ngày từ khi HR pass vòng hồ sơ sơ loại
            await _slaTrackingService.CreateTaskAsync(SlaModuleType.CandidateApproval, candidateId, ct);

            // Gửi email thông báo cho Trưởng phòng
            if (managerEmployee != null && managerEmployee.AccountId.HasValue)
            {
                var managerAccount = await _accountRepo.GetByIdAsync(managerEmployee.AccountId.Value, ct);
                if (managerAccount != null && !string.IsNullOrEmpty(managerAccount.Email))
                {
                    string jobTitleName = request.Position?.Title ?? "vị trí tuyển dụng";
                    string subject = $"[HRM] Yêu cầu phê duyệt hồ sơ ứng viên: {candidate.FullName}";
                    string body = $@"
                        <h3>Kính gửi Trưởng phòng,</h3>
                        <p>Hồ sơ của ứng viên <b>{candidate.FullName}</b> ứng tuyển vào vị trí <b>{jobTitleName}</b> đã được HR phê duyệt qua vòng hồ sơ sơ loại.</p>
                        <p>Hồ sơ hiện đã được đưa vào luồng phê duyệt và đang chờ xác nhận của bạn để tiến hành vòng phỏng vấn chuyên môn.</p>
                        <p>Vui lòng đăng nhập vào hệ thống HRM và truy cập <b>Hộp thư phê duyệt</b> để thực hiện xử lý.</p>
                        <br/>
                        <p>Trân trọng,<br/>Bộ phận Tuyển dụng HICAS</p>";
                    await _emailService.SendEmailAsync(managerAccount.Email, subject, body);
                }
            }

            await _unitOfWork.CommitAsync(ct);
            return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> ConfirmByDepartmentAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"candidate_{candidateId}", async (innerCt) =>
            {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            if (candidate.Status != CandidateStatus.Interview_Pending)
                throw new InvalidOperationException("Ho so ung vien khong o trang thai cho Truong phong duyet.");
            await EnsureManagerCanAccessCandidateAsync(candidate, approverId, actorRoleName, innerCt);
            
            await EnsureCandidateDepartmentApprovalWorkflowAsync(candidate, innerCt);
            await _approvalService.ProcessStepAsync("CANDIDATE", candidateId, approverId, actorRoleName, true, "Department Approved", innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> FinalApproveAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"candidate_{candidateId}", async (innerCt) =>
            {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            if (candidate.Status != CandidateStatus.Interview_Passed)
                throw new InvalidOperationException("Ho so ung vien chua qua buoc Truong phong duyet.");
            await EnsureManagerCanAccessCandidateAsync(candidate, approverId, actorRoleName, innerCt);

            // TRỌNG TÂM: Check SLA chặn trước theo đúng sơ đồ
            var slaTask = await _slaTrackingRepo.GetPendingTaskAsync(SlaModuleType.CandidateApproval, candidateId, ct);
            if (slaTask != null && slaTask.Deadline < DateTime.UtcNow)
            {
                candidate.Status = CandidateStatus.SLA_Expired;
                await _candidateRepo.UpdateAsync(candidate, ct);

                slaTask.Status = SlaTaskStatus.Violated;
                await _slaTrackingRepo.UpdateAsync(slaTask, ct);
                await _unitOfWork.CommitAsync(ct);

                throw new InvalidOperationException("SLA_EXPIRED: Hồ sơ đã quá hạn phê duyệt (quá 15 ngày).");
            }

            // Đẩy luồng duyệt đi tiếp (Sẽ kích hoạt CandidateApprovalCompletedHandler)
            await EnsureCandidateDirectorApprovalWorkflowAsync(candidate, innerCt);
            await _approvalService.ProcessStepAsync("CANDIDATE", candidateId, approverId, actorRoleName, true, "Director Final Approved", innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> RejectAsync(int candidateId, int actorId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"candidate_{candidateId}", async (innerCt) =>
            {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            await EnsureManagerCanAccessCandidateAsync(candidate, actorId, actorRoleName, ct);

            // Nếu đã tuyển (Hired) hoặc đã Hủy/Từ chối rồi thì không cho phép từ chối lại
            if (candidate.Status == CandidateStatus.Offer || candidate.Status == CandidateStatus.Rejected)
                throw new InvalidOperationException("Không thể từ chối hồ sơ ở trạng thái hiện tại.");

            // 1. Cập nhật trạng thái thành Rejected
            candidate.Status = CandidateStatus.Rejected;
            await _candidateRepo.UpdateAsync(candidate, ct);

            // 2. Tắt bộ đếm SLA (nếu có) để tránh cảnh báo rác cho HR
            await _slaTrackingService.ResolveTaskAsync(SlaModuleType.CandidateApproval, candidateId, ct);

            // 3. Ghi log kiểm toán (Tùy chọn)
            await _auditLogRepo.LogSystemEventAsync(
                actionType: "CANDIDATE_REJECTED",
                accountId: 0,
                module: "recruitment",
                message: $"Hồ sơ ứng viên {candidate.FullName} đã bị từ chối."
            );

            await _unitOfWork.CommitAsync(ct);
            return true;
            }, cancellationToken: ct);
        }

        private async Task EnsureManagerCanAccessCandidateAsync(Candidate candidate, int actorId, string actorRoleName, CancellationToken ct)
        {
            if (!IsManager(actorRoleName))
                return;

            var request = await _reqRepo.GetByIdAsync(candidate.RecruitmentRequestId ?? 0, ct);
            if (request == null)
                throw new InvalidOperationException("Yêu cầu tuyển dụng không tồn tại.");

            await EnsureManagerCanAccessRequestAsync(request, actorId, actorRoleName, ct);
        }

        private async Task EnsureCandidateDepartmentApprovalWorkflowAsync(Candidate candidate, CancellationToken ct)
        {
            var request = await _reqRepo.GetByIdWithCandidatesAsync(candidate.RecruitmentRequestId ?? 0, ct);
            if (request == null || !request.DeptId.HasValue)
                throw new InvalidOperationException("Khong xac dinh duoc phong ban tuyen dung cua ung vien.");

            var configuredManager = request.Department?.Manager;
            var managerAccountId = configuredManager?.AccountId;
            if (!managerAccountId.HasValue)
            {
                var managerAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Manager", ct);
                var managerEmployee = (await _employeeRepo.FindAsync(
                    e => e.DeptId == request.DeptId.Value &&
                         e.AccountId.HasValue &&
                         managerAccountIds.Contains(e.AccountId.Value),
                    ct)).FirstOrDefault();
                managerAccountId = managerEmployee?.AccountId;
            }

            if (!managerAccountId.HasValue)
                throw new InvalidOperationException("Khong tim thay Truong phong de duyet ho so ung vien.");

            var directorId = await GetDirectorApproverIdAsync(ct);
            await _approvalService.CreateWorkflowAsync("CANDIDATE", candidate.Id, new List<int> { managerAccountId.Value, directorId }, ct);
        }

        private async Task EnsureCandidateDirectorApprovalWorkflowAsync(Candidate candidate, CancellationToken ct)
        {
            var directorId = await GetDirectorApproverIdAsync(ct);
            await _approvalService.CreateWorkflowAsync("CANDIDATE", candidate.Id, new List<int> { directorId }, ct);
        }

        private async Task<int> GetDirectorApproverIdAsync(CancellationToken ct)
        {
            var directorAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
            var directorId = directorAccountIds.FirstOrDefault();
            if (directorId == 0)
                throw new InvalidOperationException("Khong tim thay Giam doc trong he thong.");

            return directorId;
        }

        private async Task EnsureManagerCanAccessRequestAsync(RecruitmentRequest request, int actorId, string actorRoleName, CancellationToken ct)
        {
            if (!IsManager(actorRoleName))
                return;

            var managerDeptId = await GetManagerDeptIdAsync(actorId, ct);
            if (!request.DeptId.HasValue || request.DeptId.Value != managerDeptId)
                throw new UnauthorizedAccessException("Manager chỉ được thao tác với dữ liệu tuyển dụng thuộc phòng ban của mình.");
        }

        private async Task<int> GetManagerDeptIdAsync(int accountId, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null || !employee.DeptId.HasValue)
                throw new UnauthorizedAccessException("Tài khoản Manager chưa được gắn với phòng ban.");

            return employee.DeptId.Value;
        }

        private static bool IsManager(string actorRoleName)
        {
            return string.Equals(actorRoleName, "Manager", StringComparison.OrdinalIgnoreCase);
        }

        private async Task EnsureJobCanReceiveApplicationsAsync(RecruitmentRequest job, CancellationToken ct)
        {
            if (job.Status == RecruitmentRequestStatus.Closed)
                throw new InvalidOperationException("Tin tuyển dụng đã được đóng.");

            if (job.Status != RecruitmentRequestStatus.Approved)
                throw new InvalidOperationException("Tin tuyển dụng này chưa được mở hoặc đã đóng.");

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Tin tuyển dụng này đã hết hạn nộp hồ sơ.");

            if (job.Quantity > 0 && CountFilledSlots(job) >= job.Quantity)
            {
                job.Status = RecruitmentRequestStatus.Closed;
                await _reqRepo.UpdateAsync(job, ct);
                await _unitOfWork.CommitAsync(ct);
                throw new InvalidOperationException("Tin tuyển dụng đã đủ số lượng cần tuyển.");
            }
        }

        private static int CountFilledSlots(RecruitmentRequest request)
        {
            return request.Candidates.Count(c => c.Status == CandidateStatus.Offer || c.Status == CandidateStatus.Hired);
        }

        private static string GenerateTrackingCode()
        {
            return $"CAND-{Guid.NewGuid():N}".Substring(0, 13).ToUpperInvariant();
        }

        private async Task SendApplicationReceiptEmailAsync(Candidate candidate, RecruitmentRequest job, string trackingCode)
        {
            if (string.IsNullOrWhiteSpace(candidate.Email))
                return;

            var candidateName = WebUtility.HtmlEncode(candidate.FullName);
            var jobTitle = WebUtility.HtmlEncode(job.Position?.Title ?? job.Description ?? "Vị trí tuyển dụng");
            var departmentName = WebUtility.HtmlEncode(job.Department?.DeptName ?? "HICAS");
            var safeTrackingCode = WebUtility.HtmlEncode(trackingCode);
            var appliedDate = DateTime.UtcNow.ToString("dd/MM/yyyy");

            var subject = $"[HICAS] Xác nhận đã nhận hồ sơ - {trackingCode}";
            var body = $@"
                <div style=""font-family:Arial,sans-serif;line-height:1.6;color:#111827"">
                    <p>Chào <b>{candidateName}</b>,</p>
                    <p>HICAS đã nhận hồ sơ ứng tuyển của bạn cho vị trí <b>{jobTitle}</b> thuộc <b>{departmentName}</b>.</p>
                    <p>Mã tra cứu hồ sơ của bạn là:</p>
                    <p style=""display:inline-block;padding:12px 16px;border:1px dashed #f58220;border-radius:8px;background:#fff7ed;color:#c2410c;font-size:20px;font-weight:700;letter-spacing:1px"">
                        {safeTrackingCode}
                    </p>
                    <p>Vui lòng lưu lại mã này để tra cứu trạng thái hồ sơ trên cổng tuyển dụng HICAS.</p>
                    <p><b>Ngày ghi nhận:</b> {appliedDate}</p>
                    <br/>
                    <p>Trân trọng,<br/>Bộ phận Tuyển dụng HICAS</p>
                </div>";

            await _emailService.SendEmailAsync(candidate.Email, subject, body);
        }

    }
}
