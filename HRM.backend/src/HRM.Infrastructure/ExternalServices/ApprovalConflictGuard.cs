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
                ?? throw new InvalidOperationException("Khong tim thay ho so nhan su.");

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
                ?? throw new InvalidOperationException("Khong tim thay ho so nhan su.");

            var hrAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("HR", ct);
            return hrAccountIds.Any(id => !employee.AccountId.HasValue || id != employee.AccountId.Value);
        }

        public async Task<int> GetDirectorAccountIdAsync(CancellationToken ct = default)
        {
            var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
            var directorId = directorIds.FirstOrDefault();

            if (directorId == 0)
                throw new InvalidOperationException("He thong chua co tai khoan Giam doc de xu ly phe duyet dac biet.");

            return directorId;
        }

        public async Task<int> GetAlternativeDirectorApproverAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Khong tim thay ho so nhan su.");

            var excludedAccountId = employee.AccountId;
            var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
            var directorId = directorIds.FirstOrDefault(id => !excludedAccountId.HasValue || id != excludedAccountId.Value);
            if (directorId != 0)
                return directorId;

            var adminIds = await _accountRepo.GetAccountIdsByRoleAsync("Admin", ct);
            var adminId = adminIds.FirstOrDefault(id => !excludedAccountId.HasValue || id != excludedAccountId.Value);
            if (adminId != 0)
                return adminId;

            throw new InvalidOperationException("Chua co Giam doc khac hoac Admin de duyet thay cho ho so cua nguoi giu vai tro Giam doc.");
        }

        public async Task EnsureNotSelfApprovalForEmployeeAsync(int employeeId, int approverAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Khong tim thay ho so nhan su.");

            EnsureNotSelfApproval(employee.AccountId, approverAccountId);
        }

        public void EnsureNotSelfApproval(int? targetAccountId, int approverAccountId)
        {
            if (targetAccountId.HasValue && targetAccountId.Value == approverAccountId)
                throw new UnauthorizedAccessException("Khong duoc tu phe duyet yeu cau lien quan den chinh minh.");
        }

        private static bool IsSpecialApprovalRole(string? roleName)
        {
            return string.Equals(roleName, "HR", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase);
        }
    }
}
