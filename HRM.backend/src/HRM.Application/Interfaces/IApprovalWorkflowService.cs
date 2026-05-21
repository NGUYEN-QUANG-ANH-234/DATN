using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IApprovalWorkflowService
    {
        // Khởi tạo luồng duyệt (Truyền vào danh sách ID người duyệt theo thứ tự cấp bậc)
        Task<int> CreateWorkflowAsync(string moduleCode, int referenceId, List<int> approverAccountIds, CancellationToken ct = default);

        // Người duyệt thao tác
        Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,     // 1. Thêm mã module (Ví dụ: "RECRUITMENT")
            int referenceId,       // Đổi tên từ requestId thành referenceId cho rõ nghĩa
            int approverAccountId,
            string actorRoleName,
            bool isApproved,
            string? note = null,
            CancellationToken ct = default);

        // Lấy danh sách các đơn đang chờ người dùng hiện tại duyệt
        Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(
            int approverId, 
            string actorRoleName, 
            CancellationToken ct = default);
    }
}
