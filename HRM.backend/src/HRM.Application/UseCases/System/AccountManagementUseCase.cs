using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class AccountManagementUseCase : IAccountManagementUseCase
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILockService _lockService;

        public AccountManagementUseCase(
            IAccountRepository accountRepo,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILockService lockService)
        {
            _accountRepo = accountRepo;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _lockService = lockService;
        }

        public async Task<CreateAccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default)
        {
            var email = dto.Email.Trim();
            return await _lockService.GetWithLockAsync($"account_create_{email.ToLowerInvariant()}", async (innerCt) =>
            {
                var existingUser = await _accountRepo.GetByEmailAsync(email, innerCt);
                if (existingUser != null)
                    throw new Exception("Email nay da duoc su dung trong he thong.");

                var isGeneratedPassword = string.IsNullOrWhiteSpace(dto.Password);
                string rawPassword = isGeneratedPassword
                    ? GenerateSecurePassword()
                    : dto.Password!.Trim();
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);
                var roleId = dto.RoleId > 0 ? dto.RoleId : 8;

                var newAccount = new Account
                {
                    Email = email,
                    FullName = dto.FullName,
                    RoleId = roleId,
                    PasswordHash = hashedPassword,
                    Status = AccountStatus.Active,
                    IsMfaEnabled = false
                };

                await _accountRepo.AddAsync(newAccount, innerCt);
                await _unitOfWork.CommitAsync(innerCt);

                await _emailService.SendEmailAsync(email, "Tài khoản HICAS của bạn",
                    $"Tài khoản: {email}\nMật khẩu tạm thời: {rawPassword}\nVui lòng đổi mật khẩu sau khi đăng nhập.");
                await _unitOfWork.CommitAsync(innerCt);

                return new CreateAccountResultDto
                {
                    AccountId = newAccount.Id,
                    TemporaryPassword = isGeneratedPassword ? rawPassword : null,
                    IsGeneratedPassword = isGeneratedPassword
                };
            }, cancellationToken: ct);
        }

        public async Task ToggleAccountStatusAsync(int accountId, AccountStatus newStatus, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"account_{accountId}", async (innerCt) =>
            {
                var account = await _accountRepo.GetByIdAsync(accountId, innerCt);
                if (account == null) throw new Exception("Tài khoản không tồn tại.");

                account.Status = newStatus;

                if (newStatus == AccountStatus.Locked || newStatus == AccountStatus.Suspended)
                {
                    RevokeUserSessions(account);
                }

                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<ResetPasswordResultDto> ResetPasswordManuallyAsync(int accountId, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"account_{accountId}", async (innerCt) =>
            {
                var account = await _accountRepo.GetByIdAsync(accountId, innerCt);
                if (account == null) throw new Exception("Tài khoản không tồn tại.");

                string newRawPassword = GenerateSecurePassword();
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newRawPassword);
                RevokeUserSessions(account);

                await _unitOfWork.CommitAsync(innerCt);

                await _emailService.SendEmailAsync(account.Email, "Mật khẩu HICAS đã được cấp lại",
                    $"Mật khẩu mới của bạn là: {newRawPassword}");
                await _unitOfWork.CommitAsync(innerCt);

                return new ResetPasswordResultDto
                {
                    TemporaryPassword = newRawPassword
                };
            }, cancellationToken: ct);
        }

        public async Task UpdateAccountRoleAsync(int accountId, int newRoleId, CancellationToken ct = default)
        {
            await UpdateAccountRoleAsync(accountId, newRoleId, 0, ct);
        }

        public async Task UpdateAccountRoleAsync(int targetAccountId, int newRoleId, int actorId, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"account_{targetAccountId}", async (innerCt) =>
            {
                var targetAccount = await _accountRepo.GetByIdAsync(targetAccountId, innerCt);
                if (targetAccount == null) throw new Exception("Tài khoản không tồn tại.");

                var roles = (await _accountRepo.FetchRolesWithPermissionMatrixAsync(innerCt)).ToList();
                var targetRole = roles.FirstOrDefault(r => r.Id == targetAccount.RoleId);
                var newRole = roles.FirstOrDefault(r => r.Id == newRoleId);
                var targetIsAdmin = string.Equals(targetRole?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
                var newRoleIsAdmin = string.Equals(newRole?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

                if (actorId > 0 && targetAccountId == actorId && targetIsAdmin && !newRoleIsAdmin)
                    throw new Exception("Bạn không thể tự hạ cấp quyền Admin của chính mình.");

                if (targetIsAdmin && !newRoleIsAdmin)
                {
                    var adminCount = (await _accountRepo.GetAllWithRoleAsync(innerCt))
                        .Count(a => string.Equals(a.Role?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) &&
                                    a.Status == AccountStatus.Active);
                    if (adminCount <= 1)
                        throw new Exception("Hệ thống phải có ít nhất một tài khoản Admin đang hoạt động.");
                }

                targetAccount.RoleId = newRoleId;
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<IEnumerable<AccountListItemDto>> GetAllAccountsAsync(CancellationToken ct = default)
        {
            var accounts = await _accountRepo.GetAllWithRoleAsync(ct);

            return accounts.Select(account => new AccountListItemDto
            {
                Id = account.Id,
                Email = account.Email,
                FullName = account.FullName ?? string.Empty,
                RoleId = account.RoleId,
                RoleName = account.Role?.RoleName ?? string.Empty,
                Status = account.Status.ToString(),
                IsMfaEnabled = account.IsMfaEnabled,
                CreatedAt = account.CreatedAt,
                AvatarUrl = account.AvatarUrl
            });
        }

        private static string GenerateSecurePassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8) + "@Hicas!";
        }

        private static void RevokeUserSessions(Account account)
        {
            account.RefreshToken = null;
            account.RefreshTokenExpiryTime = DateTime.MinValue;
        }
    }
}
