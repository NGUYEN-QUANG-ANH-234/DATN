using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Services; // Thêm namespace cho SLA Service
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class ManageProfileUseCase : IManageProfileUseCase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<ProfileUpdateRequest> _profileRequestRepo;
        private readonly IStorageService _storageService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IContractRepository _contractRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly ISlaTrackingRepository _slaRepo;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public ManageProfileUseCase(
            IEmployeeRepository employeeRepo,
            IBaseRepository<ProfileUpdateRequest> profileRequestRepo,
            IStorageService storageService,
            IAuditLogRepository auditLogRepo,
            IContractRepository contractRepo,
            ISlaTrackingService slaTrackingService,
            ISlaTrackingRepository slaRepo,
            IApprovalWorkflowService approvalWorkflowService,
            IApprovalConflictGuard approvalConflictGuard,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _employeeRepo = employeeRepo;
            _profileRequestRepo = profileRequestRepo;
            _storageService = storageService;
            _auditLogRepo = auditLogRepo;
            _contractRepo = contractRepo;
            _slaTrackingService = slaTrackingService;
            _slaRepo = slaRepo;
            _approvalWorkflowService = approvalWorkflowService;
            _approvalConflictGuard = approvalConflictGuard;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<int> RequestProfileUpdateAsync(int accountId, ProfileUpdateRequestDto dto, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null) throw new ArgumentException("Tài khoản chưa liên kết hồ sơ.");

            return await _lockService.GetWithLockAsync($"profile_update_create_{employee.Id}", async (innerCt) =>
            {
            int newRequestId = 0;

            // BỌC TRONG GIAO DỊCH (TRANSACTION)
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                int actualEmployeeId = employee.Id;

                if (!string.IsNullOrEmpty(dto.IdentityNumber))
                {
                    var isConflict = await _employeeRepo.CheckIdentityNumberExistsAsync(dto.IdentityNumber, actualEmployeeId, innerCt);
                    if (isConflict) throw new InvalidOperationException("CONFLICT_IDENTITY");
                }

                var existingRequests = await _profileRequestRepo.FindAsync(r =>
                    r.EmployeeId == actualEmployeeId &&
                    (r.Status == RequestStatus.Pending_HR || r.Status == RequestStatus.Pending_Director), innerCt);
                if (existingRequests.Any()) throw new InvalidOperationException("Đang có yêu cầu chờ duyệt.");

                var isHrTarget = await _approvalConflictGuard.IsEmployeeInRoleAsync(actualEmployeeId, "HR", innerCt);
                var requiresDirectorApproval = isHrTarget &&
                    !await _approvalConflictGuard.HasAlternativeHrApproverAsync(actualEmployeeId, innerCt);

                var updatePayload = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(dto.FullName)) updatePayload["FullName"] = dto.FullName;
                if (dto.Gender.HasValue) updatePayload["Gender"] = (int)dto.Gender.Value;
                if (dto.BirthDate.HasValue) updatePayload["BirthDate"] = dto.BirthDate.Value;
                if (!string.IsNullOrEmpty(dto.Nationality)) updatePayload["Nationality"] = dto.Nationality;
                if (!string.IsNullOrEmpty(dto.Ethnicity)) updatePayload["Ethnicity"] = dto.Ethnicity;
                if (!string.IsNullOrEmpty(dto.PhoneNumber)) updatePayload["PhoneNumber"] = dto.PhoneNumber;
                if (!string.IsNullOrEmpty(dto.PersonalEmail)) updatePayload["PersonalEmail"] = dto.PersonalEmail;
                if (!string.IsNullOrEmpty(dto.CurrentAddress)) updatePayload["CurrentAddress"] = dto.CurrentAddress;
                if (!string.IsNullOrEmpty(dto.PermanentAddress)) updatePayload["PermanentAddress"] = dto.PermanentAddress;
                if (!string.IsNullOrEmpty(dto.IdentityNumber)) updatePayload["IdentityNumber"] = dto.IdentityNumber;
                if (!string.IsNullOrEmpty(dto.TaxCode)) updatePayload["TaxCode"] = dto.TaxCode;
                if (!string.IsNullOrEmpty(dto.SocialInsCode)) updatePayload["SocialInsCode"] = dto.SocialInsCode;
                if (dto.SocialInsJoinDate.HasValue) updatePayload["SocialInsJoinDate"] = dto.SocialInsJoinDate.Value;
                if (!string.IsNullOrEmpty(dto.InsuranceHospital)) updatePayload["InsuranceHospital"] = dto.InsuranceHospital;
                if (!string.IsNullOrEmpty(dto.BankAccount)) updatePayload["BankAccount"] = dto.BankAccount;
                if (!string.IsNullOrEmpty(dto.BankName)) updatePayload["BankName"] = dto.BankName;
                if (!string.IsNullOrEmpty(dto.EmergencyContactName)) updatePayload["EmergencyContactName"] = dto.EmergencyContactName;
                if (!string.IsNullOrEmpty(dto.EmergencyPhone)) updatePayload["EmergencyPhone"] = dto.EmergencyPhone;
                if (!string.IsNullOrEmpty(dto.EmergencyRelation)) updatePayload["EmergencyRelation"] = dto.EmergencyRelation;

                if (dto.AvatarFile != null) updatePayload["AvatarUrl"] = await _storageService.UploadFileAsync(dto.AvatarFile, "avatars", innerCt);
                if (dto.IdentityFrontFile != null) updatePayload["IdentityFrontUrl"] = await _storageService.UploadFileAsync(dto.IdentityFrontFile, "evidences", innerCt);
                if (dto.IdentityBackFile != null) updatePayload["IdentityBackUrl"] = await _storageService.UploadFileAsync(dto.IdentityBackFile, "evidences", innerCt);
                if (dto.CertificateFile != null) updatePayload["CertificateUrl"] = await _storageService.UploadFileAsync(dto.CertificateFile, "evidences", innerCt);

                if (updatePayload.Count == 0) throw new ArgumentException("Không có dữ liệu nào được yêu cầu cập nhật.");

                var requestEntity = new ProfileUpdateRequest
                {
                    EmployeeId = actualEmployeeId,
                    RequestedDataJson = JsonSerializer.Serialize(updatePayload),
                    Status = requiresDirectorApproval ? RequestStatus.Pending_Director : RequestStatus.Pending_HR,
                    CreatedAt = DateTime.UtcNow
                };

                // Lệnh này giờ chỉ lưu nháp vào Context, chưa chốt hẳn vào DB nếu chưa xong Transaction
                await _profileRequestRepo.AddAsync(requestEntity, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                newRequestId = requestEntity.Id;

                // Bọc try-catch nhẹ cho SLA, nếu chưa cấu hình SLA trong DB thì không làm sập cả luồng Update
                try
                {
                    await _slaTrackingService.CreateTaskAsync(SlaModuleType.ProfileUpdate, requestEntity.Id, innerCt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CẢNH BÁO] Không thể tạo SLA Task: {ex.Message}");
                }

                await _auditLogRepo.LogSystemEventAsync("REQUEST_PROFILE_UPDATE", accountId, "employee_profile", "Nhân viên nộp minh chứng và yêu cầu cập nhật hồ sơ");
                await _unitOfWork.CommitAsync(innerCt);

            }, innerCt); // Transaction tự động Commit an toàn tại đây

            return newRequestId;
            }, cancellationToken: ct);
        }

        // 4. HÀM DÀNH CHO HR DUYỆT (Tách riêng biệt)
        public async Task<bool> ReviewProfileUpdateAsync(int requestId, int hrAccountId, string actorRoleName, ReviewProfileUpdateDto dto, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"profile_update_{requestId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var requestEntity = await _profileRequestRepo.GetByIdAsync(requestId, innerCt);
                if (requestEntity == null ||
                    (requestEntity.Status != RequestStatus.Pending_HR && requestEntity.Status != RequestStatus.Pending_Director))
                    throw new ArgumentException("Yêu cầu không tồn tại hoặc đã được xử lý.");

                if (requestEntity.Status == RequestStatus.Pending_Director)
                    EnsureDirectorOrAdmin(actorRoleName);
                else
                    EnsureHrOrAdmin(actorRoleName);

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(requestEntity.EmployeeId, hrAccountId, innerCt);

                if (!dto.IsApproved)
                {
                    requestEntity.Status = RequestStatus.Rejected;
                    requestEntity.RejectReason = dto.RejectReason;
                }
                else
                {
                    var employee = await _employeeRepo.GetByIdAsync(requestEntity.EmployeeId, innerCt);
                    if (employee == null) throw new ArgumentException("Không tìm thấy nhân viên.");

                    // Đọc JSON và ghi đè trực tiếp
                    var updateData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestEntity.RequestedDataJson);
                    if (updateData != null)
                    {
                        if (updateData.ContainsKey("FullName")) employee.FullName = updateData["FullName"].GetString()!;
                        if (updateData.ContainsKey("Gender")) employee.Gender = (Gender)updateData["Gender"].GetInt32();
                        if (updateData.ContainsKey("BirthDate")) employee.BirthDate = updateData["BirthDate"].GetDateTime();
                        if (updateData.ContainsKey("Nationality")) employee.Nationality = updateData["Nationality"].GetString();
                        if (updateData.ContainsKey("Ethnicity")) employee.Ethnicity = updateData["Ethnicity"].GetString();
                        if (updateData.ContainsKey("PhoneNumber")) employee.PhoneNumber = updateData["PhoneNumber"].GetString();
                        if (updateData.ContainsKey("PersonalEmail")) employee.PersonalEmail = updateData["PersonalEmail"].GetString();
                        if (updateData.ContainsKey("CurrentAddress")) employee.CurrentAddress = updateData["CurrentAddress"].GetString();
                        if (updateData.ContainsKey("PermanentAddress")) employee.PermanentAddress = updateData["PermanentAddress"].GetString();
                        if (updateData.ContainsKey("IdentityNumber")) employee.IdentityNumber = updateData["IdentityNumber"].GetString();
                        if (updateData.ContainsKey("TaxCode")) employee.TaxCode = updateData["TaxCode"].GetString();
                        if (updateData.ContainsKey("SocialInsCode")) employee.SocialInsCode = updateData["SocialInsCode"].GetString();
                        if (updateData.ContainsKey("SocialInsJoinDate")) employee.SocialInsJoinDate = updateData["SocialInsJoinDate"].GetDateTime();
                        if (updateData.ContainsKey("InsuranceHospital")) employee.InsuranceHospital = updateData["InsuranceHospital"].GetString();
                        if (updateData.ContainsKey("BankAccount")) employee.BankAccount = updateData["BankAccount"].GetString();
                        if (updateData.ContainsKey("BankName")) employee.BankName = updateData["BankName"].GetString();
                        if (updateData.ContainsKey("EmergencyContactName")) employee.EmergencyContactName = updateData["EmergencyContactName"].GetString();
                        if (updateData.ContainsKey("EmergencyPhone")) employee.EmergencyPhone = updateData["EmergencyPhone"].GetString();
                        if (updateData.ContainsKey("EmergencyRelation")) employee.EmergencyRelation = updateData["EmergencyRelation"].GetString();

                        if (updateData.ContainsKey("AvatarUrl")) employee.AvatarUrl = updateData["AvatarUrl"].GetString();
                        if (updateData.ContainsKey("IdentityFrontUrl")) employee.IdentityFrontUrl = updateData["IdentityFrontUrl"].GetString();
                        if (updateData.ContainsKey("IdentityBackUrl")) employee.IdentityBackUrl = updateData["IdentityBackUrl"].GetString();
                        if (updateData.ContainsKey("CertificateUrl")) employee.CertificateUrl = updateData["CertificateUrl"].GetString();
                    }

                    requestEntity.Status = RequestStatus.Approved;
                }

                await _profileRequestRepo.UpdateAsync(requestEntity, innerCt);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ProfileUpdate, requestEntity.Id, innerCt);

                string actionLog = dto.IsApproved ? "PROFILE_UPDATE_APPROVED" : "PROFILE_UPDATE_REJECTED";
                await _auditLogRepo.LogSystemEventAsync(actionLog, hrAccountId, "employee_profile", $"Xử lý hồ sơ ID {requestId}");
                await _unitOfWork.CommitAsync(innerCt);

            }, innerCt);

            return true;
            }, cancellationToken: ct);
        }

        public async Task<MyProfileDto?> GetMyProfileAsync(int accountId, CancellationToken ct = default)
        {
            // Fix lỗi truyền nhầm ID: Lấy EmployeeId thật từ AccountId
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null) return null;

            var emp = await _employeeRepo.GetProfileByIdAsync(employee.Id, ct);
            if (emp == null) return null;

            return new MyProfileDto
            {
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                Gender = emp.Gender?.ToString() ?? "Khác",
                BirthDate = emp.BirthDate?.ToString("yyyy-MM-dd"),
                Nationality = emp.Nationality,
                Ethnicity = emp.Ethnicity,

                // --- MAP CÁC TRƯỜNG MỚI ---
                PhoneNumber = emp.PhoneNumber,
                PersonalEmail = emp.PersonalEmail,
                CurrentAddress = emp.CurrentAddress,
                PermanentAddress = emp.PermanentAddress,

                IdentityNumber = emp.IdentityNumber,
                TaxCode = emp.TaxCode,
                SocialInsCode = emp.SocialInsCode,
                SocialInsJoinDate = emp.SocialInsJoinDate?.ToString("yyyy-MM-dd"),
                InsuranceHospital = emp.InsuranceHospital,

                BankAccount = emp.BankAccount,
                BankName = emp.BankName,

                EmergencyContactName = emp.EmergencyContactName,
                EmergencyPhone = emp.EmergencyPhone,
                EmergencyRelation = emp.EmergencyRelation,
                // --------------------------

                JoinedDate = emp.JoinedDate?.ToString("yyyy-MM-dd"),
                AvatarUrl = emp.AvatarUrl,
                IdentityFrontUrl = emp.IdentityFrontUrl,
                IdentityBackUrl = emp.IdentityBackUrl,
                CertificateUrl = emp.CertificateUrl,
                Status = emp.Status.ToString()
            };
        }

        public async Task<List<MyContractDto>> GetMyContractsAsync(int accountId, CancellationToken ct = default)
        {
            // Fix lỗi truyền nhầm ID: Lấy EmployeeId thật từ AccountId
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (employee == null) return new List<MyContractDto>();

            var list = await _contractRepo.GetByEmployeeIdAsync(employee.Id, ct);

            return list.Select(c => new MyContractDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ContractType = c.ContractType.ToString(),
                BasicSalary = c.BasicSalary,
                SalaryPercentage = c.SalaryPercentage,
                InsuranceSalary = c.InsuranceSalary,
                StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                EndDate = c.EndDate?.ToString("yyyy-MM-dd"),
                Status = c.Status.ToString(),
                Version = c.Version,
                NegotiationNote = c.NegotiationNote
            }).ToList();
        }
       

        public async Task<List<PendingProfileRequestDto>> GetPendingProfileRequestsAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            // 1. Lấy tất cả Request đang Pending
            var pendingStatuses = IsDirector(actorRoleName)
                ? new[] { RequestStatus.Pending_Director }
                : IsAdmin(actorRoleName)
                    ? new[] { RequestStatus.Pending_HR, RequestStatus.Pending_Director }
                    : new[] { RequestStatus.Pending_HR };

            var pendingRequests = await _profileRequestRepo.FindAsync(r => pendingStatuses.Contains(r.Status), ct);
            if (!pendingRequests.Any()) return new List<PendingProfileRequestDto>();

            var employeeIds = pendingRequests.Select(r => r.EmployeeId).Distinct().ToList();
            var requestIds = pendingRequests.Select(r => r.Id).ToList();

            // 2. Lấy thông tin Tên, Mã NV (Dùng FindAsync có sẵn của BaseRepository)
            var employees = await _employeeRepo.FindAsync(e => employeeIds.Contains(e.Id), ct);
            var empDict = employees.ToDictionary(e => e.Id);

            // 3. Lấy hạn chót từ bảng SLA Trung tâm
            var slaTasks = await _slaRepo.FindAsync(s =>
                s.ModuleType == SlaModuleType.ProfileUpdate &&
                requestIds.Contains(s.ReferenceId) &&
                s.Status == SlaTaskStatus.Pending, ct);
            var slaDict = slaTasks.ToDictionary(s => s.ReferenceId);

            if (IsHr(actorRoleName) && !IsAdmin(actorRoleName))
            {
                pendingRequests = pendingRequests
                    .Where(r => !empDict.TryGetValue(r.EmployeeId, out var employee) ||
                                !employee.AccountId.HasValue ||
                                employee.AccountId.Value != actorAccountId)
                    .ToList();
            }

            // 4. Lắp ráp và trả về
            return pendingRequests.Select(r => new PendingProfileRequestDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = empDict.ContainsKey(r.EmployeeId) ? empDict[r.EmployeeId].FullName : "Không xác định",
                EmployeeCode = empDict.ContainsKey(r.EmployeeId) ? empDict[r.EmployeeId].EmployeeCode : "N/A",
                RequestedDataJson = r.RequestedDataJson,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                // Ưu tiên lấy Deadline từ SLA Tracking, nếu không có lấy mặc định +72h
                DeadlineSLA = slaDict.ContainsKey(r.Id) ? slaDict[r.Id].Deadline : r.CreatedAt.AddHours(72)
            })
            .OrderBy(r => r.DeadlineSLA) // Sắp xếp cái nào sắp hết hạn lên đầu để HR duyệt trước
            .ToList();
        }

        private static void EnsureHrOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được duyệt yêu cầu cập nhật hồ sơ thông thường.");
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được duyệt yêu cầu cập nhật hồ sơ khi không có HR khác xử lý.");
        }

        private static bool IsHr(string role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
    }
}
