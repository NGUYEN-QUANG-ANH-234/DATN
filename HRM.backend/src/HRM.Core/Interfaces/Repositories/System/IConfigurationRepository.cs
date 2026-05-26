using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IConfigurationRepository : IBaseRepository<Configuration>
    {
        // ==========================================
        // 1. SALARY VARIABLE REPO (Biến lương)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchVariableMappingsAsync(CancellationToken ct = default);
        Task SaveMappingAsync(string code, string tablePath, string? description = null, CancellationToken ct = default);

        // ==========================================
        // 2. SYSTEM CONFIG REPO (SLA & Điểm danh)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchSLAByModuleAsync(CancellationToken ct = default);
        Task UpdateSLAConfigAsync(string moduleCode, string value, string unit, CancellationToken ct = default);
        Task<IEnumerable<Configuration>> FetchLatestConfigAsync(CancellationToken ct = default);
        Task SaveAttendanceParamsAsync(string configJsonValue, CancellationToken ct = default);

        // ==========================================
        // 3. TEMPLATE REPO (Mẫu thông báo)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchAllTemplatesAsync(CancellationToken ct = default);
        Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson, CancellationToken ct = default);

        // ==========================================
        // 4. DOCUMENT EXPORT TEMPLATE REPO
        // ==========================================
        Task<IEnumerable<Configuration>> FetchDocumentTemplatesAsync(CancellationToken ct = default);
    }
}
