using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class OnboardingUseCase : IOnboardingUseCase
    {
        private readonly IBaseRepository<OnboardingRequest> _onboardingRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ICandidateRepository _candidateRepo;
        private readonly IDepartmentRepository _departmentRepo;
        private readonly IPositionRepository _positionRepo;
        private readonly IStorageService _storageService;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILockService _lockService;
        private readonly IEmailService _emailService;

        public OnboardingUseCase(
            IBaseRepository<OnboardingRequest> onboardingRepo,
            IEmployeeRepository employeeRepo,
            IAccountRepository accountRepo,
            ICandidateRepository candidateRepo,
            IDepartmentRepository departmentRepo,
            IPositionRepository positionRepo,
            IStorageService storageService,
            ISlaTrackingService slaTrackingService,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILockService lockService,
            IEmailService emailService)
        {
            _onboardingRepo = onboardingRepo;
            _employeeRepo = employeeRepo;
            _accountRepo = accountRepo;
            _candidateRepo = candidateRepo;
            _departmentRepo = departmentRepo;
            _positionRepo = positionRepo;
            _storageService = storageService;
            _slaTrackingService = slaTrackingService;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _lockService = lockService;
            _emailService = emailService;
        }

        public async Task<OnboardingCandidateLookupDto> ResolveCandidateAsync(ResolveOnboardingCandidateDto dto, CancellationToken ct = default)
        {
            var candidate = await ResolveCandidateForProfileSetupAsync(dto.Email, dto.TrackingCode, ct);
            var recruitmentRequest = candidate.RecruitmentRequest;

            return new OnboardingCandidateLookupDto
            {
                CandidateId = candidate.Id,
                TrackingCode = candidate.TrackingCode ?? string.Empty,
                Email = candidate.Email ?? dto.Email.Trim(),
                FullName = candidate.FullName,
                Status = candidate.Status.ToString(),
                RecruitmentRequestId = candidate.RecruitmentRequestId,
                DepartmentId = recruitmentRequest?.DeptId,
                DepartmentName = recruitmentRequest?.Department?.DeptName,
                PositionId = recruitmentRequest?.PositionId,
                PositionName = recruitmentRequest?.Position?.Title
            };
        }

        public async Task SubmitProfileAsync(SubmitOnboardingDto dto, CancellationToken ct = default)
        {
            var normalizedEmail = NormalizeEmail(dto.Email);
            var normalizedTrackingCode = NormalizeTrackingCode(dto.TrackingCode);

            await _lockService.GetWithLockAsync($"onboarding_submit_{normalizedTrackingCode}_{normalizedEmail}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var candidate = await ResolveCandidateForProfileSetupAsync(dto.Email, dto.TrackingCode, innerCt);
                    if (dto.CandidateId > 0 && dto.CandidateId != candidate.Id)
                        throw new InvalidOperationException("Thông tin hồ sơ không khớp với mã tra cứu.");

                    var existingOnboarding = (await _onboardingRepo.FindAsync(
                            r => r.CandidateId == candidate.Id,
                            innerCt))
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefault();

                    if (existingOnboarding?.Status == OnboardingStatus.Pending_HR ||
                        existingOnboarding?.Status == OnboardingStatus.Completed)
                    {
                        throw new InvalidOperationException(
                            existingOnboarding.Status == OnboardingStatus.Completed
                                ? "Hồ sơ tiếp nhận này đã được HR kích hoạt."
                                : "Hồ sơ tiếp nhận đã được gửi và đang chờ HR xác minh.");
                    }

                    var accountEmail = !string.IsNullOrWhiteSpace(candidate.Email)
                        ? candidate.Email.Trim()
                        : dto.Email.Trim();

                    string idFront = await _storageService.UploadFileAsync(dto.IdentityFrontFile, "evidences", innerCt);
                    string idBack = await _storageService.UploadFileAsync(dto.IdentityBackFile, "evidences", innerCt);
                    string? cert = dto.CertificateFile != null ? await _storageService.UploadFileAsync(dto.CertificateFile, "evidences", innerCt) : null;

                    var data = new Dictionary<string, object>
                    {
                        { "FullName", dto.FullName },
                        { "Email", accountEmail },
                        { "PhoneNumber", dto.PhoneNumber },
                        { "PersonalEmail", dto.PersonalEmail },
                        { "CurrentAddress", dto.CurrentAddress },
                        { "PermanentAddress", dto.PermanentAddress },
                        { "IdentityNumber", dto.IdentityNumber },
                        { "EmergencyContactName", dto.EmergencyContactName },
                        { "EmergencyPhone", dto.EmergencyPhone },
                        { "EmergencyRelation", dto.EmergencyRelation },
                        { "IdentityFrontUrl", idFront },
                        { "IdentityBackUrl", idBack },
                        { "CertificateUrl", cert ?? "" },
                        { "Gender", dto.Gender ?? "" },
                        { "BirthDate", dto.BirthDate?.ToString("yyyy-MM-dd") ?? "" },
                        { "Nationality", dto.Nationality ?? "" },
                        { "Ethnicity", dto.Ethnicity ?? "" },
                        { "TaxCode", dto.TaxCode ?? "" },
                        { "SocialInsCode", dto.SocialInsCode ?? "" },
                        { "BankAccount", dto.BankAccount ?? "" },
                        { "BankName", dto.BankName ?? "" },
                    };

                    var request = existingOnboarding ?? new OnboardingRequest
                    {
                        CandidateId = candidate.Id,
                        RequestedDataJson = "{}",
                        Status = OnboardingStatus.PendingCandidateProfile
                    };

                    request.RequestedDataJson = JsonSerializer.Serialize(data);
                    request.Status = OnboardingStatus.Pending_HR;

                    if (existingOnboarding == null)
                        await _onboardingRepo.AddAsync(request, innerCt);
                    else
                        await _onboardingRepo.UpdateAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    await _slaTrackingService.CreateTaskAsync(SlaModuleType.Onboarding, request.Id, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task ReviewByHrAsync(int requestId, ReviewOnboardingDto dto, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"onboarding_{requestId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                var request = await _onboardingRepo.GetByIdAsync(requestId, innerCt);
                if (request == null || request.Status != OnboardingStatus.Pending_HR)
                    throw new InvalidOperationException("Hồ sơ không tồn tại hoặc đã được xử lý.");

                if (!dto.IsApproved)
                {
                    request.Status = OnboardingStatus.Rejected;
                    request.RejectReason = dto.RejectReason;
                    await _onboardingRepo.UpdateAsync(request, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("HR_Reject_New_Hire", 0, "onboarding", $"Từ chối Onboarding ID {requestId}");
                }
                else
                {
                    // Kiểm tra bắt buộc phải chọn vai trò khi phê duyệt
                    if (!dto.RoleId.HasValue)
                        throw new ArgumentException("Vui lòng chỉ định vai trò (Role) hệ thống cho nhân viên mới.");

                    var candidate = (await _candidateRepo.GetCandidatesWithDetailsAsync(new List<int> { request.CandidateId }, innerCt))
                        .FirstOrDefault();
                    if (candidate == null)
                        throw new InvalidOperationException("Không tìm thấy ứng viên liên kết với hồ sơ onboarding.");

                    var resolvedDepartmentId = dto.DepartmentId ?? candidate?.RecruitmentRequest?.DeptId;
                    var resolvedPositionId = dto.PositionId ?? candidate?.RecruitmentRequest?.PositionId;

                    if (!resolvedDepartmentId.HasValue || resolvedDepartmentId.Value <= 0)
                        throw new ArgumentException("Vui lòng chọn phòng ban trước khi kích hoạt nhân viên.");

                    if (!resolvedPositionId.HasValue || resolvedPositionId.Value <= 0)
                        throw new ArgumentException("Vui lòng chọn vị trí/chức danh trước khi kích hoạt nhân viên.");

                    await ValidateInitialOrganizationAsync(resolvedDepartmentId.Value, resolvedPositionId.Value, innerCt);

                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.RequestedDataJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (data == null) throw new InvalidOperationException("Dữ liệu hồ sơ bị lỗi.");

                    string email = GetStringValue(data, "Email")
                        ?? GetStringValue(data, "PersonalEmail")
                        ?? candidate!.Email
                        ?? throw new InvalidOperationException("Hồ sơ chưa có email tài khoản để kích hoạt nhân viên.");
                    email = email.Trim();
                    string fullName = GetStringValue(data, "FullName")
                        ?? candidate!.FullName
                        ?? throw new InvalidOperationException("Hồ sơ chưa có họ tên để kích hoạt nhân viên.");
                    string empCode = $"NV{DateTime.Now.Year}{new Random().Next(1000, 9999)}";

                    // 1. Tìm hoặc tạo Account. Ứng viên ngoài hệ thống có thể chưa có tài khoản nội bộ.
                    var normalizedEmail = email.ToLowerInvariant();
                    var existingAccount = (await _accountRepo.FindAsync(
                        a => a.Email.ToLower() == normalizedEmail,
                        innerCt)).FirstOrDefault();
                    var temporaryPassword = existingAccount == null ? GenerateSecurePassword() : null;

                    if (existingAccount is null)
                    {
                        existingAccount = new Account
                        {
                            Email = email,
                            FullName = fullName,
                            RoleId = dto.RoleId.Value,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                            Status = AccountStatus.Active,
                            IsMfaEnabled = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _accountRepo.AddAsync(existingAccount, innerCt);
                    }
                    else
                    {
                        // Gán RoleId theo lựa chọn thực tế của HR
                        existingAccount.RoleId = dto.RoleId.Value;
                        existingAccount.FullName = fullName;
                        existingAccount.Status = AccountStatus.Active;
                        await _accountRepo.UpdateAsync(existingAccount, innerCt);
                    }
                    await _unitOfWork.CommitAsync(innerCt);

                    if (!string.IsNullOrWhiteSpace(temporaryPassword))
                    {
                        await _emailService.SendEmailAsync(
                            email,
                            "Tài khoản HICAS của bạn",
                            $"Tài khoản: {email}\nMật khẩu tạm thời: {temporaryPassword}\nVui lòng đăng nhập và đổi mật khẩu sau khi truy cập hệ thống.");
                        await _unitOfWork.CommitAsync(innerCt);
                    }

                    // Tự động map EmployeeType tương ứng với RoleId để đồng bộ dữ liệu hồ sơ
                    EmployeeType empType = dto.RoleId.Value switch
                    {
                        7 => EmployeeType.Intern,        // Thực tập sinh
                        6 => EmployeeType.Contractual,   // Cộng tác viên
                        _ => EmployeeType.Probation      // Nhân viên, Quản lý, HR (Vào thử việc trước)
                    };

                    // 2. Tạo Employee chính thức
                    var employee = new Employee
                    {
                        AccountId = existingAccount.Id,
                        CandidateId = request.CandidateId,
                        EmployeeCode = empCode,
                        FullName = fullName,
                        PersonalEmail = GetStringValue(data, "PersonalEmail") ?? email,
                        Nationality = GetStringValue(data, "Nationality"),
                        Ethnicity = GetStringValue(data, "Ethnicity"),
                        PhoneNumber = GetStringValue(data, "PhoneNumber"),
                        CurrentAddress = GetStringValue(data, "CurrentAddress"),
                        PermanentAddress = GetStringValue(data, "PermanentAddress"),
                        IdentityNumber = GetStringValue(data, "IdentityNumber"),
                        EmergencyContactName = GetStringValue(data, "EmergencyContactName"),
                        EmergencyPhone = GetStringValue(data, "EmergencyPhone"),
                        EmergencyRelation = GetStringValue(data, "EmergencyRelation"),

                        IdentityFrontUrl = GetStringValue(data, "IdentityFrontUrl"),
                        IdentityBackUrl = GetStringValue(data, "IdentityBackUrl"),
                        CertificateUrl = GetStringValue(data, "CertificateUrl"),

                        Type = empType, // Đồng bộ loại hình nhân sự
                        Status = EmployeeStatus.Probation,
                        DeptId = resolvedDepartmentId.Value,
                        PositionId = resolvedPositionId.Value,
                        JoinedDate = DateTime.UtcNow.Date,

                        Gender = int.TryParse(GetStringValue(data, "Gender"), out int g) ? (Gender)g : null,
                        BirthDate = DateTime.TryParse(GetStringValue(data, "BirthDate"), out DateTime dob) ? dob : null,
                        TaxCode = GetStringValue(data, "TaxCode"),
                        SocialInsCode = GetStringValue(data, "SocialInsCode"),
                        BankAccount = GetStringValue(data, "BankAccount"),
                        BankName = GetStringValue(data, "BankName"),
                    };
                    await _employeeRepo.AddAsync(employee, innerCt);

                    // 3. Hoàn tất quy trình
                    candidate!.Status = CandidateStatus.Hired;
                    await _candidateRepo.UpdateAsync(candidate, innerCt);

                    request.Status = OnboardingStatus.Completed;
                    await _onboardingRepo.UpdateAsync(request, innerCt);

                    await _auditLogRepo.LogSystemEventAsync(
                        "HR_Approve_And_Activate_Employee",
                        0,
                        "onboarding",
                        $"Kích hoạt nhân viên {empCode} với RoleId {dto.RoleId.Value}, DeptId {resolvedDepartmentId.Value}, PositionId {resolvedPositionId.Value}");
                    await _unitOfWork.CommitAsync(innerCt);

                    await _mediator.Publish(new OnboardingCompletedEvent
                    {
                        EmployeeId = employee.Id,
                        EmpCode = empCode,
                        Email = email,
                        FullName = fullName
                    }, innerCt);
                }

                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.Onboarding, requestId, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<IEnumerable<PendingOnboardingRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default)
        {
            var requests = await _onboardingRepo.FindAsync(
                r => r.Status == OnboardingStatus.Pending_HR,
                ct);

            var orderedRequests = requests.OrderBy(r => r.CreatedAt).ToList();
            var candidateIds = orderedRequests
                .Select(r => r.CandidateId)
                .Distinct()
                .ToList();
            var candidates = candidateIds.Count == 0
                ? new Dictionary<int, Candidate>()
                : (await _candidateRepo.GetCandidatesWithDetailsAsync(candidateIds, ct))
                    .ToDictionary(c => c.Id);

            return orderedRequests
                .Select(r =>
                {
                    candidates.TryGetValue(r.CandidateId, out var candidate);
                    var recruitmentRequest = candidate?.RecruitmentRequest;

                    return new PendingOnboardingRequestDto
                    {
                        Id = r.Id,
                        CandidateId = r.CandidateId,
                        RecruitmentRequestId = candidate?.RecruitmentRequestId,
                        DepartmentId = recruitmentRequest?.DeptId,
                        DepartmentName = recruitmentRequest?.Department?.DeptName,
                        PositionId = recruitmentRequest?.PositionId,
                        PositionName = recruitmentRequest?.Position?.Title,
                        RequestedDataJson = r.RequestedDataJson,
                        Status = r.Status.ToString(),
                        CreatedAt = r.CreatedAt
                    };
                });
        }

        private async Task ValidateInitialOrganizationAsync(int departmentId, int positionId, CancellationToken ct)
        {
            var department = await _departmentRepo.GetByIdAsync(departmentId, ct);
            if (department == null || department.Status != DeptStatus.Active)
                throw new ArgumentException("Phòng ban được chọn không tồn tại hoặc đã ngừng hoạt động.");

            var position = await _positionRepo.GetByIdAsync(positionId, ct);
            if (position == null || !position.IsActive)
                throw new ArgumentException("Vị trí/chức danh được chọn không tồn tại hoặc đã ngừng sử dụng.");
        }

        private async Task<Candidate> ResolveCandidateForProfileSetupAsync(string email, string trackingCode, CancellationToken ct)
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedTrackingCode = NormalizeTrackingCode(trackingCode);

            var matched = (await _candidateRepo.FindAsync(
                    c => c.Email != null &&
                         c.Email.ToLower() == normalizedEmail &&
                         c.TrackingCode != null &&
                         c.TrackingCode.ToUpper() == normalizedTrackingCode,
                    ct))
                .OrderByDescending(c => c.AppliedDate)
                .FirstOrDefault();

            if (matched == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ ứng tuyển khớp với email và mã tra cứu.");

            var candidate = (await _candidateRepo.GetCandidatesWithDetailsAsync(new List<int> { matched.Id }, ct))
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ ứng tuyển.");

            if (candidate.Status != CandidateStatus.Offer)
            {
                throw new InvalidOperationException(candidate.Status == CandidateStatus.Hired
                    ? "Hồ sơ này đã được HR kích hoạt."
                    : "Hồ sơ chưa ở trạng thái sẵn sàng tiếp nhận. Vui lòng chờ HR xác nhận kết quả tuyển dụng.");
            }

            return candidate;
        }

        private static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Vui lòng nhập email ứng tuyển.");

            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizeTrackingCode(string trackingCode)
        {
            if (string.IsNullOrWhiteSpace(trackingCode))
                throw new ArgumentException("Vui lòng nhập mã tra cứu hồ sơ.");

            return trackingCode.Trim().ToUpperInvariant();
        }

        private static string? GetStringValue(Dictionary<string, JsonElement> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || value.ValueKind == JsonValueKind.Null)
                return null;

            return value.GetString();
        }

        private static string GenerateSecurePassword()
        {
            return Guid.NewGuid().ToString("N")[..8] + "@Hicas!";
        }
    }
}
