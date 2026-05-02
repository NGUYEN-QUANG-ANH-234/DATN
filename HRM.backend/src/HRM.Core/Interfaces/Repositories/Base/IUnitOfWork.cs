namespace HRM.backend.src.HRM.Core.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        // Lưu các thay đổi thông thường
        Task<int> CommitAsync();

        // Quản lý giao dịch (Dành cho các nghiệp vụ phức tạp cần rollback)
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
