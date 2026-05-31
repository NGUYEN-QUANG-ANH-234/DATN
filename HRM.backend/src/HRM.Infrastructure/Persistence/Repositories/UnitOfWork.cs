using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(MyDbContext context)
        {
            _context = context;
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(ct);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            // Lấy chiến lược thực thi hiện tại của MySQL (có hỗ trợ Retry)
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Khởi tạo Transaction bên trong Strategy
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    // Chạy khối lệnh UseCase truyền vào
                    await action();

                    // Commit nếu không có lỗi
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }

        public void Dispose(CancellationToken ct = default)
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
