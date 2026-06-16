using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances
{
    public interface IExternalTimesheetImportRepository
    {
        Task<List<ExternalTimesheetImport>> GetBatchesAsync(
            byte? month,
            short? year,
            ExternalTimesheetImportStatus? status,
            CancellationToken ct = default);

        Task<ExternalTimesheetImport?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<ExternalTimesheetImport?> GetTrackedDetailAsync(int id, CancellationToken ct = default);
        Task<List<ExternalTimesheetLine>> GetDuplicateCandidatesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<ExternalTimesheetLine>> GetReplaceableDuplicateCandidatesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task AddAsync(ExternalTimesheetImport import, CancellationToken ct = default);
        void Update(ExternalTimesheetImport import);
        void RemoveLines(IEnumerable<ExternalTimesheetLine> lines);
    }
}
