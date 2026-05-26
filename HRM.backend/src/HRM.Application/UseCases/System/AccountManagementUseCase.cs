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

        public async Task<int> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default)
        {
            var email = dto.Email.Trim();
            return await _lockService.GetWithLockAsync($"account_create_{email.ToLowerInvariant()}", async (innerCt) =>
            {
                var existingUser = await _accountRepo.GetByEmailAsync(email, innerCt);
                if (existingUser != null)
                    throw new Exception("Email nay da duoc su dung trong he thong.");

                string rawPassword = string.IsNullOrWhiteSpace(dto.Password)
                    ? GenerateSecurePassword()
                    : dto.Password.Trim();
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

                await _emailService.SendEmailAsync(email, "Tai khoan HICAS cua ban",
                    $"Tai khoan: {email}\nMat khau tam thoi: {rawPassword}\nVui long doi mat khau sau khi dang nhap.");
                await _unitOfWork.CommitAsync(innerCt);

                return newAccount.Id;
            }, cancellationToken: ct);
        }

        public async Task ToggleAccountStatusAsync(int accountId, AccountStatus newStatus, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"account_{accountId}", async (innerCt) =>
            {
                var account = await _accountRepo.GetByIdAsync(accountId, innerCt);
                if (account == null) throw new Exception("Tai khoan khong ton tai.");

                account.Status = newStatus;

                if (newStatus == AccountStatus.Locked || newStatus == AccountStatus.Suspended)
                {
                    RevokeUserSessions(account);
                }

                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task ResetPasswordManuallyAsync(int accountId, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"account_{accountId}", async (innerCt) =>
            {
                var account = await _accountRepo.GetByIdAsync(accountId, innerCt);
                if (account == null) throw new Exception("Tai khoan khong ton tai.");

                string newRawPassword = GenerateSecurePassword();
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newRawPassword);
                RevokeUserSessions(account);

                await _unitOfWork.CommitAsync(innerCt);

                await _emailService.SendEmailAsync(account.Email, "Mat khau HICAS da duoc cap lai",
                    $"Mat khau moi cua ban la: {newRawPassword}");
                await _unitOfWork.CommitAsync(innerCt);
                return true;
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
                if (targetAccount == null) throw new Exception("Tai khoan khong ton tai.");

                if (actorId > 0 && targetAccountId == actorId && targetAccount.RoleId == 1 && newRoleId != 1)
                    throw new Exception("Ban khong the tu ha cap quyen Admin cua chinh minh.");

                if (targetAccount.RoleId == 1 && newRoleId != 1)
                {
                    var adminCount = (await _accountRepo.GetAllAsync(innerCt)).Count(a => a.RoleId == 1 && a.Status == AccountStatus.Active);
                    if (adminCount <= 1)
                        throw new Exception("He thong phai co it nhat mot tai khoan Admin dang hoat dong.");
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
