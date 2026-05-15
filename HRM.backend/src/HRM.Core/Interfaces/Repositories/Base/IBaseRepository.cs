using System.Linq.Expressions;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        // Lấy tất cả dữ liệu
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

        // Lấy dữ liệu theo ID
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

        // Lấy dữ liệu theo điều kiện động (LINQ)
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, CancellationToken ct = default);

        // Thêm mới
        Task AddAsync(T entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

        // Cập nhật (EF Core thường tracking nên dùng void đồng bộ)
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        // Xóa
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}