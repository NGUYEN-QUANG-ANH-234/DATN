using HRM.backend.src.HRM.Core.Entities.Recruitment;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment
{
    public interface ICandidateRepository : IBaseRepository<Candidate>
    {
        Task<List<Candidate>> GetCandidatesWithDetailsAsync(List<int> ids, CancellationToken ct = default);
    }
}
