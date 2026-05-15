using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IMfaRecoveryCodeRepository : IBaseRepository<MfaRecoveryCode>
    {
        // Hàm lưu nhiều mã khôi phục cùng lúc
        Task AddBulkAsync(IEnumerable<MfaRecoveryCode> codes, CancellationToken ct = default);

        // Hàm này sẽ dùng sau này khi user đăng nhập bằng mã khôi phục
        Task<MfaRecoveryCode?> GetUnusedCodeAsync(int accountId, string code, CancellationToken ct = default);
        Task DeleteAllUserCodesAsync(int userId, CancellationToken ct = default);
    }
}
