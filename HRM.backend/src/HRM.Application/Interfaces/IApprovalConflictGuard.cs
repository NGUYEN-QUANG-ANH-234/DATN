namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IApprovalConflictGuard
    {
        Task<bool> RequiresDirectorApprovalAsync(int employeeId, CancellationToken ct = default);
        Task<string?> GetEmployeeRoleNameAsync(int employeeId, CancellationToken ct = default);
        Task<bool> IsEmployeeInRoleAsync(int employeeId, string roleName, CancellationToken ct = default);
        Task<bool> HasAlternativeHrApproverAsync(int employeeId, CancellationToken ct = default);
        Task<int> GetDirectorAccountIdAsync(CancellationToken ct = default);
        Task EnsureNotSelfApprovalForEmployeeAsync(int employeeId, int approverAccountId, CancellationToken ct = default);
        void EnsureNotSelfApproval(int? targetAccountId, int approverAccountId);
    }
}
