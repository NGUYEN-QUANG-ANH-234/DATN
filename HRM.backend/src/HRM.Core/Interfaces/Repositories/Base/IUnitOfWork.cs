namespace HRM.backend.src.HRM.Core.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        // Lưu các thay đổi thông thường
        Task<int> CommitAsync(CancellationToken ct = default);

        // Quản lý giao dịch (Dành cho các nghiệp vụ phức tạp cần rollback)
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task ExecuteTransactionAsync(Func<Task> action, CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}
