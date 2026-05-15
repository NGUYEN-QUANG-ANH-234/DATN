using HRM.backend.src.HRM.Application.DTOs;
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

        public AccountManagementUseCase(IAccountRepository accountRepo, IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _accountRepo = accountRepo;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<int> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default)
        {
            var existingUser = await _accountRepo.GetByEmailAsync(dto.Email, ct);
            if (existingUser != null)
                throw new Exception("Email này đã được sử dụng trong hệ thống.");

            string rawPassword = GenerateSecurePassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            var newAccount = new Account
            {
                Email = dto.Email,
                FullName = dto.FullName,
                RoleId = dto.RoleId,
                PasswordHash = hashedPassword,
                Status = AccountStatus.Active,
                IsMfaEnabled = false
            };

            await _accountRepo.AddAsync(newAccount, ct);

            // Tự động ghi Log qua ChangeTracker trong MyDbContext
            await _unitOfWork.CommitAsync(ct);

            // Gửi mail mật khẩu khởi tạo (Fire and Forget)
            _ = _emailService.SendEmailAsync(dto.Email, "Tài khoản HICAS của bạn",
                $"Tài khoản: {dto.Email}\nMật khẩu tạm thời: {rawPassword}\nVui lòng đổi mật khẩu sau khi đăng nhập.");

            return newAccount.Id;
        }

        public async Task ToggleAccountStatusAsync(int accountId, AccountStatus newStatus, CancellationToken ct = default)
        {
            var account = await _accountRepo.GetByIdAsync(accountId, ct);
            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            account.Status = newStatus;

            // BẢO MẬT: Nếu khóa tài khoản, lập tức đá văng user bằng cách thu hồi Refresh Token
            if (newStatus == AccountStatus.Locked || newStatus == AccountStatus.Suspended)
            {
                RevokeUserSessions(account);
            }

            await _unitOfWork.CommitAsync(ct);
        }

        public async Task ResetPasswordManuallyAsync(int accountId, CancellationToken ct = default)
        {
            var account = await _accountRepo.GetByIdAsync(accountId, ct);
            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            string newRawPassword = GenerateSecurePassword();
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newRawPassword);

            // BẢO MẬT: Bắt buộc đăng nhập lại sau khi Admin đổi mật khẩu
            RevokeUserSessions(account);

            await _unitOfWork.CommitAsync(ct);

            _ = _emailService.SendEmailAsync(account.Email, "Mật khẩu HICAS đã được cấp lại",
                $"Mật khẩu mới của bạn là: {newRawPassword}");
        }

        public async Task UpdateAccountRoleAsync(int accountId, int newRoleId, CancellationToken ct = default)
        {
            var account = await _accountRepo.GetByIdAsync(accountId, ct);
            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            // Gán Role mới
            account.RoleId = newRoleId;

            // MyDbContext sẽ tự động log lại việc đổi Role này
            await _unitOfWork.CommitAsync(ct);
        }

        // --- HÀM HỖ TRỢ NỘI BỘ ---

        private string GenerateSecurePassword()
        {
            // Sinh mật khẩu ngẫu nhiên 8 ký tự an toàn
            return Guid.NewGuid().ToString("N").Substring(0, 8) + "@Hicas!";
        }

        private void RevokeUserSessions(Account account)
        {
            // Xóa RefreshToken để khi AccessToken hết hạn (hoặc gọi refresh), user sẽ bị yêu cầu đăng nhập lại
            account.RefreshToken = null;
            account.RefreshTokenExpiryTime = DateTime.MinValue;
        }

        public async Task UpdateAccountRoleAsync(int targetAccountId, int newRoleId, int actorId, CancellationToken ct = default)
        {
            // 1. Kiểm tra tài khoản mục tiêu
            var targetAccount = await _accountRepo.GetByIdAsync(targetAccountId, ct);
            if (targetAccount == null) throw new Exception("Tài khoản không tồn tại.");

            // 2. XỬ LÝ KHÉO: Ngăn chặn tự hạ cấp bản thân nếu là Admin
            if (targetAccountId == actorId && targetAccount.RoleId == 1 && newRoleId != 1)
            {
                throw new Exception("Bạn không thể tự hạ cấp quyền Admin của chính mình để tránh mất quyền quản trị.");
            }

            // 3. XỬ LÝ KHÉO: Kiểm tra xem có phải là Admin cuối cùng không
            if (targetAccount.RoleId == 1 && newRoleId != 1)
            {
                var adminCount = (await _accountRepo.GetAllAsync(ct)).Count(a => a.RoleId == 1 && a.Status == AccountStatus.Active);
                if (adminCount <= 1)
                {
                    throw new Exception("Hệ thống phải có ít nhất một tài khoản Admin đang hoạt động.");
                }
            }

            targetAccount.RoleId = newRoleId;
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task<IEnumerable<Account>> GetAllAccountsAsync(CancellationToken ct = default)
        {
            return await _accountRepo.GetAllAsync(ct); // Đảm bảo AccountRepo đã có hàm GetAll
        }
    }
}
