using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
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
        private readonly IStorageService _storageService;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILockService _lockService;

        public OnboardingUseCase(
            IBaseRepository<OnboardingRequest> onboardingRepo,
            IEmployeeRepository employeeRepo,
            IAccountRepository accountRepo,
            IStorageService storageService,
            ISlaTrackingService slaTrackingService,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILockService lockService)
        {
            _onboardingRepo = onboardingRepo;
            _employeeRepo = employeeRepo;
            _accountRepo = accountRepo;
            _storageService = storageService;
            _slaTrackingService = slaTrackingService;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _lockService = lockService;
        }

        public async Task SubmitProfileAsync(SubmitOnboardingDto dto, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"onboarding_submit_{dto.CandidateId}_{dto.Email.Trim().ToLowerInvariant()}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    string idFront = await _storageService.UploadFileAsync(dto.IdentityFrontFile, "evidences", innerCt);
                    string idBack = await _storageService.UploadFileAsync(dto.IdentityBackFile, "evidences", innerCt);
                    string? cert = dto.CertificateFile != null ? await _storageService.UploadFileAsync(dto.CertificateFile, "evidences", innerCt) : null;

                    var data = new Dictionary<string, object>
                    {
                        { "FullName", dto.FullName },
                        { "Email", dto.Email },
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
                        { "TaxCode", dto.TaxCode ?? "" },
                        { "SocialInsCode", dto.SocialInsCode ?? "" },
                        { "BankAccount", dto.BankAccount ?? "" },
                        { "BankName", dto.BankName ?? "" },
                    };

                    var request = new OnboardingRequest
                    {
                        CandidateId = dto.CandidateId,
                        RequestedDataJson = JsonSerializer.Serialize(data),
                        Status = OnboardingStatus.Pending_HR
                    };

                    await _onboardingRepo.AddAsync(request, innerCt);
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

                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.RequestedDataJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (data == null) throw new InvalidOperationException("Dữ liệu hồ sơ bị lỗi.");

                    string email = data["Email"].GetString()!;
                    string fullName = data["FullName"].GetString()!;
                    string empCode = $"NV{DateTime.Now.Year}{new Random().Next(1000, 9999)}";

                    // 1. Tìm Account gốc (Role mặc định ban đầu là 8 - Ứng viên)
                    var existingAccount = (await _accountRepo.FindAsync(a => a.Email == email, innerCt)).FirstOrDefault();
                    if (existingAccount == null)
                        throw new InvalidOperationException("Không tìm thấy tài khoản liên kết với Email này.");

                    // 🔥 CẬP NHẬT ĐỘNG: Gán RoleId theo lựa chọn thực tế của HR
                    existingAccount.RoleId = dto.RoleId.Value;
                    await _accountRepo.UpdateAsync(existingAccount, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);

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
                        PersonalEmail = email,
                        PhoneNumber = data.ContainsKey("PhoneNumber") ? data["PhoneNumber"].GetString() : null,
                        CurrentAddress = data.ContainsKey("CurrentAddress") ? data["CurrentAddress"].GetString() : null,
                        PermanentAddress = data.ContainsKey("PermanentAddress") ? data["PermanentAddress"].GetString() : null,
                        IdentityNumber = data.ContainsKey("IdentityNumber") ? data["IdentityNumber"].GetString() : null,
                        EmergencyContactName = data.ContainsKey("EmergencyContactName") ? data["EmergencyContactName"].GetString() : null,
                        EmergencyPhone = data.ContainsKey("EmergencyPhone") ? data["EmergencyPhone"].GetString() : null,
                        EmergencyRelation = data.ContainsKey("EmergencyRelation") ? data["EmergencyRelation"].GetString() : null,

                        IdentityFrontUrl = data.ContainsKey("IdentityFrontUrl") ? data["IdentityFrontUrl"].GetString() : null,
                        IdentityBackUrl = data.ContainsKey("IdentityBackUrl") ? data["IdentityBackUrl"].GetString() : null,
                        CertificateUrl = data.ContainsKey("CertificateUrl") ? data["CertificateUrl"].GetString() : null,

                        Type = empType, // Đồng bộ loại hình nhân sự
                        Status = EmployeeStatus.Probation,

                        Gender = data.ContainsKey("Gender") && int.TryParse(data["Gender"].GetString(), out int g) ? (Gender)g : null,
                        BirthDate = data.ContainsKey("BirthDate") && DateTime.TryParse(data["BirthDate"].GetString(), out DateTime dob) ? dob : null,
                        TaxCode = data.ContainsKey("TaxCode") ? data["TaxCode"].GetString() : null,
                        SocialInsCode = data.ContainsKey("SocialInsCode") ? data["SocialInsCode"].GetString() : null,
                        BankAccount = data.ContainsKey("BankAccount") ? data["BankAccount"].GetString() : null,
                        BankName = data.ContainsKey("BankName") ? data["BankName"].GetString() : null,
                    };
                    await _employeeRepo.AddAsync(employee, innerCt);

                    // 3. Hoàn tất quy trình
                    request.Status = OnboardingStatus.Completed;
                    await _onboardingRepo.UpdateAsync(request, innerCt);

                    await _auditLogRepo.LogSystemEventAsync("HR_Approve_And_Activate_Employee", 0, "onboarding", $"Kích hoạt nhân viên {empCode} với RoleId {dto.RoleId.Value}");
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

            return requests
                .OrderBy(r => r.CreatedAt)
                .Select(r => new PendingOnboardingRequestDto
                {
                    Id = r.Id,
                    CandidateId = r.CandidateId,
                    RequestedDataJson = r.RequestedDataJson,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                });
        }
    }
}
