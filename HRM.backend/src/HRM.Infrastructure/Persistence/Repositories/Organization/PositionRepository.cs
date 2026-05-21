using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Organization
{
    public class PositionRepository : BaseRepository<Position>, IPositionRepository
    {
        public PositionRepository(MyDbContext context) : base(context) { }

        public async Task<List<Position>> GetAllActivePositionsAsync(CancellationToken ct = default)
        {
            return await _dbSet.Where(p => p.IsActive).AsNoTracking().ToListAsync(ct);
        }

        public async Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Where(p => p.IsActive)
                .AsNoTracking() // Tăng tốc độ query vì chỉ đọc dữ liệu
                .ToListAsync(ct);
        }
    }
}
