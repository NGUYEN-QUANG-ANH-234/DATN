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
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;

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
            IUnitOfWork unitOfWork)
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
        }

        public async Task<ApplyJobResultDto> ApplyForJobAsync(ApplyJobDto dto, CancellationToken ct = default)
        {
            // 1. Kiểm tra nghiệp vụ: Tin tuyển dụng
            var job = await _reqRepo.GetByIdAsync(dto.RecruitmentRequestId, ct);
            if (job == null)
                throw new InvalidOperationException("Tin tuyển dụng không tồn tại.");

            if (job.Status != RecruitmentRequestStatus.Approved)
                throw new InvalidOperationException("Tin tuyển dụng này chưa được mở hoặc đã đóng.");

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Tin tuyển dụng này đã hết hạn nộp hồ sơ.");

            // 2. Tìm kiếm ứng viên theo Email và Job
            var existingCandidate = (await _candidateRepo.FindAsync(c =>
                c.RecruitmentRequestId == dto.RecruitmentRequestId &&
                c.Email != null && c.Email.ToLower() == dto.Email.ToLower(), ct)).FirstOrDefault();

            // 3. Upload CV mới
            string newCvUrl = await _storageService.UploadFileAsync(dto.CvFile, "cvs", ct);

            string generatedTrackingCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

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

                await _candidateRepo.UpdateAsync(existingCandidate, ct);
                await _unitOfWork.CommitAsync(ct);

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

                await _candidateRepo.AddAsync(newCandidate, ct);
                await _unitOfWork.CommitAsync(ct);

                return new ApplyJobResultDto { CandidateId = newCandidate.Id, TrackingCode = generatedTrackingCode };
            }
        }

        public async Task<IEnumerable<CandidateHistoryDto>> GetMyApplicationsAsync(string email, string? trackingCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return Enumerable.Empty<CandidateHistoryDto>();

            var candidates = await _candidateRepo.FindAsync(c => c.Email != null && c.Email.ToLower() == email.ToLower() && 
                (string.IsNullOrEmpty(trackingCode) || c.TrackingCode == trackingCode), ct);
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
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            if (candidate.Status != CandidateStatus.New) throw new InvalidOperationException("Hồ sơ đã được xử lý trước đó.");

            var request = await _reqRepo.GetByIdAsync(candidate.RecruitmentRequestId ?? 0, ct);
            if (request == null || !request.DeptId.HasValue) throw new InvalidOperationException("Yêu cầu tuyển dụng không xác định được phòng ban.");
            await EnsureManagerCanAccessRequestAsync(request, actorId, actorRoleName, ct);

            var managerAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Manager", ct);
            var managerEmployee = (await _employeeRepo.FindAsync(e => e.DeptId == request.DeptId && e.AccountId.HasValue && managerAccountIds.Contains(e.AccountId.Value), ct)).FirstOrDefault();
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
                    _ = _emailService.SendEmailAsync(managerAccount.Email, subject, body);
                }
            }

            await _unitOfWork.CommitAsync(ct);
            return true;
        }

        public async Task<bool> ConfirmByDepartmentAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default)
        {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            await EnsureManagerCanAccessCandidateAsync(candidate, approverId, actorRoleName, ct);
            
            await _approvalService.ProcessStepAsync("CANDIDATE", candidateId, approverId, actorRoleName, true, "Department Approved", ct);
            return true;
        }

        public async Task<bool> FinalApproveAsync(int candidateId, int approverId, string actorRoleName, CancellationToken ct = default)
        {
            var candidate = await _candidateRepo.GetByIdAsync(candidateId, ct);
            if (candidate == null) throw new InvalidOperationException("Không tìm thấy ứng viên.");
            await EnsureManagerCanAccessCandidateAsync(candidate, approverId, actorRoleName, ct);

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
            await _approvalService.ProcessStepAsync("CANDIDATE", candidateId, approverId, actorRoleName, true, "Director Final Approved", ct);
            return true;
        }

        public async Task<bool> RejectAsync(int candidateId, int actorId, string actorRoleName, CancellationToken ct = default)
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

    }
}
