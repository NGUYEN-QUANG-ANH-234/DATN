using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services
{
    public interface IPersonnelChangeAccessGuard
    {
        Task<Employee> EnsureCanAccessEmployeeAsync(
            int employeeId,
            int actorAccountId,
            CancellationToken ct,
            bool requireActive = true);

        Task EnsureActiveDepartmentAsync(int? departmentId, CancellationToken ct);
        Task EnsureActivePositionAsync(int? positionId, CancellationToken ct);
        Task EnsureActiveJobLevelAsync(int? jobLevelId, CancellationToken ct);

        Task EnsureCanUseManagerAsync(int? managerId, int actorAccountId, CancellationToken ct);

        Task EnsurePerformanceReviewBelongsToEmployeeAsync(
            int? performanceReviewId,
            int employeeId,
            CancellationToken ct);

        Task EnsurePenaltyRecordBelongsToEmployeeAsync(
            int? penaltyRecordId,
            int employeeId,
            CancellationToken ct);

        Task EnsureContractBelongsToEmployeeAsync(
            int? contractId,
            int employeeId,
            CancellationToken ct);

        Task EnsurePlacementReferencesAsync(
            int? departmentId,
            int? positionId,
            int? managerId,
            int? jobLevelId,
            int actorAccountId,
            CancellationToken ct);

        void EnsurePersonnelChangeEvidencePath(string? evidenceFilePath);
    }
}
