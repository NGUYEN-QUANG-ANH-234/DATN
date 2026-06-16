using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IApprovalWorkflowService
    {
        Task<int> CreateWorkflowAsync(
            string moduleCode,
            int referenceId,
            List<int> approverAccountIds,
            CancellationToken ct = default);

        Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            bool isApproved,
            string? note = null,
            CancellationToken ct = default);

        Task<ApprovalStatus> ProcessStepAsync(
            string moduleCode,
            int referenceId,
            int approverAccountId,
            string actorRoleName,
            ApprovalWorkflowAction action,
            string? note = null,
            CancellationToken ct = default);

        Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(
            int approverId,
            string actorRoleName,
            CancellationToken ct = default);
    }
}
