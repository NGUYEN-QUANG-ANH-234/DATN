using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class ProfileUpdateApprovalHandler : INotificationHandler<ApprovalCompletedEvent>
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<ProfileUpdateRequest> _profileRequestRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly ISlaTrackingService _slaTrackingService;

        public ProfileUpdateApprovalHandler(
            IEmployeeRepository employeeRepo,
            IBaseRepository<ProfileUpdateRequest> profileRequestRepo,
            IAuditLogRepository auditLogRepo,
            ISlaTrackingService slaTrackingService)
        {
            _employeeRepo = employeeRepo;
            _profileRequestRepo = profileRequestRepo;
            _auditLogRepo = auditLogRepo;
            _slaTrackingService = slaTrackingService;
        }

        public async Task Handle(ApprovalCompletedEvent notification, CancellationToken ct)
        {
            // Chỉ bắt các sự kiện của module PROFILE_UPDATE
            if (notification.ModuleCode != "PROFILE_UPDATE") return;

            var requestEntity = await _profileRequestRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (requestEntity == null) return;

            if (notification.FinalStatus == ApprovalStatus.NeedMoreInfo)
            {
                await _auditLogRepo.LogSystemEventAsync(
                    "PROFILE_UPDATE_NEED_MORE_INFO",
                    requestEntity.EmployeeId,
                    "employee_profile",
                    $"Yêu cầu cập nhật hồ sơ ID {requestEntity.Id} cần bổ sung thông tin.");
                return;
            }

            // 1. Nếu bị từ chối ở bất kỳ cấp nào
            if (notification.FinalStatus == ApprovalStatus.Rejected)
            {
                requestEntity.Status = RequestStatus.Rejected;
                await _auditLogRepo.LogSystemEventAsync("PROFILE_UPDATE_REJECTED", requestEntity.EmployeeId, "employee_profile", $"Yêu cầu cập nhật hồ sơ ID {requestEntity.Id} đã bị từ chối.");
            }
            // 2. Nếu đã được duyệt qua TẤT CẢ các cấp -> Ghi đè dữ liệu
            else if (notification.FinalStatus == ApprovalStatus.Approved)
            {
                requestEntity.Status = RequestStatus.Approved;

                var employee = await _employeeRepo.GetByIdAsync(requestEntity.EmployeeId, ct);
                if (employee != null)
                {
                    // Ghi đè dữ liệu từ JSON nháp sang Entity chính thức
                    var updateData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestEntity.RequestedDataJson);
                    if (updateData != null)
                    {
                        if (updateData.ContainsKey("FullName")) employee.FullName = updateData["FullName"].GetString()!;
                        if (updateData.ContainsKey("Gender")) employee.Gender = (Gender)updateData["Gender"].GetInt32();
                        if (updateData.ContainsKey("BirthDate")) employee.BirthDate = updateData["BirthDate"].GetDateTime();

                        // --- ĐỌC CÁC TRƯỜNG MỚI TỪ JSON VÀ GHI VÀO DB ---
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
                        // ---------------------------------------------------

                        if (updateData.ContainsKey("AvatarUrl")) employee.AvatarUrl = updateData["AvatarUrl"].GetString();
                        if (updateData.ContainsKey("IdentityFrontUrl")) employee.IdentityFrontUrl = updateData["IdentityFrontUrl"].GetString();
                        if (updateData.ContainsKey("IdentityBackUrl")) employee.IdentityBackUrl = updateData["IdentityBackUrl"].GetString();
                        if (updateData.ContainsKey("CertificateUrl")) employee.CertificateUrl = updateData["CertificateUrl"].GetString();
                    }
                }

                await _auditLogRepo.LogSystemEventAsync("PROFILE_UPDATE_APPROVED", requestEntity.EmployeeId, "employee_profile", $"Hồ sơ ID {requestEntity.Id} đã được duyệt và cập nhật.");
            }

            await _profileRequestRepo.UpdateAsync(requestEntity, ct);

            // Đóng SLA Task khi quy trình kết thúc (dù Approved hay Rejected)
            await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ProfileUpdate, requestEntity.Id, ct);
        }
    }
}
