using Microsoft.EntityFrameworkCore;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Recruitment
{
    public class CandidateRepository : BaseRepository<Candidate>, ICandidateRepository
    {
        public CandidateRepository(MyDbContext context) : base(context) { }

        public async Task<List<Candidate>> GetCandidatesWithDetailsAsync(List<int> ids, CancellationToken ct = default)
        {
            return await _context.Candidates
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r.Department)
                        .ThenInclude(d => d!.Manager)
                            .ThenInclude(m => m!.Account)
                .Include(c => c.RecruitmentRequest)
                    .ThenInclude(r => r.Position)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct);
        }
    }
}
