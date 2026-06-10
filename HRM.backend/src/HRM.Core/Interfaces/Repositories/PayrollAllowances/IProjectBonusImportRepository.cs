using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances
{
    public interface IProjectBonusImportRepository
    {
        Task<List<ProjectBonusImportBatch>> GetBatchesAsync(
            byte? month,
            short? year,
            ProjectBonusImportStatus? status,
            CancellationToken ct = default);

        Task<ProjectBonusImportBatch?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<ProjectBonusImportBatch?> GetTrackedDetailAsync(int id, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetDuplicateCandidatesAsync(byte month, short year, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetReplaceableDuplicateCandidatesAsync(byte month, short year, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetApprovedLinesAsync(byte month, short year, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetApprovedLinesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default);
        Task AddAsync(ProjectBonusImportBatch batch, CancellationToken ct = default);
        void Update(ProjectBonusImportBatch batch);
        void RemoveLines(IEnumerable<ProjectBonusImportLine> lines);
    }
}
