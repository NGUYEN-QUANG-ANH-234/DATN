using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class ApprovalConflictGuard : IApprovalConflictGuard
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAccountRepository _accountRepo;

        public ApprovalConflictGuard(
            IEmployeeRepository employeeRepo,
            IAccountRepository accountRepo)
        {
            _employeeRepo = employeeRepo;
            _accountRepo = accountRepo;
        }

        public async Task<bool> RequiresDirectorApprovalAsync(int employeeId, CancellationToken ct = default)
        {
            var roleName = await GetEmployeeRoleNameAsync(employeeId, ct);

            return IsSpecialApprovalRole(roleName);
        }

        public async Task<string?> GetEmployeeRoleNameAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân sự.");

            if (!employee.AccountId.HasValue)
                return null;

            var account = await _accountRepo.GetByIdWithRoleAsync(employee.AccountId.Value, ct);
            return account?.Role?.RoleName;
        }

        public async Task<bool> IsEmployeeInRoleAsync(int employeeId, string roleName, CancellationToken ct = default)
        {
            var employeeRole = await GetEmployeeRoleNameAsync(employeeId, ct);
            return string.Equals(employeeRole, roleName, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> HasAlternativeHrApproverAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân sự.");

            var hrAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("HR", ct);
            return hrAccountIds.Any(id => !employee.AccountId.HasValue || id != employee.AccountId.Value);
        }

        public async Task<int> GetDirectorAccountIdAsync(CancellationToken ct = default)
        {
            var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
            var directorId = directorIds.FirstOrDefault();

            if (directorId == 0)
                throw new InvalidOperationException("Hệ thống chưa có tài khoản Giám đốc để xử lý phê duyệt đặc biệt.");

            return directorId;
        }

        public async Task EnsureNotSelfApprovalForEmployeeAsync(int employeeId, int approverAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân sự.");

            EnsureNotSelfApproval(employee.AccountId, approverAccountId);
        }

        public void EnsureNotSelfApproval(int? targetAccountId, int approverAccountId)
        {
            if (targetAccountId.HasValue && targetAccountId.Value == approverAccountId)
                throw new UnauthorizedAccessException("Không được tự phê duyệt yêu cầu liên quan đến chính mình.");
        }

        private static bool IsSpecialApprovalRole(string? roleName)
        {
            return string.Equals(roleName, "HR", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase);
        }
    }
}
