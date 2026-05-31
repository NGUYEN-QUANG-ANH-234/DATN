using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile
{
    public interface IDependentRepository : IBaseRepository<Dependent>
    {
        Task<List<Dependent>> GetByEmployeeIdAsync(int employeeId, bool includeInactive = false, CancellationToken ct = default);
        Task<Dependent?> GetByIdForEmployeeAsync(int id, int employeeId, CancellationToken ct = default);
    }
}
