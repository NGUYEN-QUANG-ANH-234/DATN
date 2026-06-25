using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IConfigurationRepository : IBaseRepository<Configuration>
    {
        // ==========================================
        // 1. SALARY VARIABLE REPO (Biến lương)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchVariableMappingsAsync(CancellationToken ct = default);
        Task EnsureVariableMappingsAsync(IEnumerable<(string Code, string Source, string Description)> variables, CancellationToken ct = default);
        Task SaveMappingAsync(string code, string tablePath, string? description = null, bool isActive = true, CancellationToken ct = default);
        Task SetMappingActiveAsync(string code, bool isActive, CancellationToken ct = default);
        Task DeleteMappingAsync(string code, CancellationToken ct = default);

        // ==========================================
        // 2. SYSTEM CONFIG REPO (SLA & Điểm danh)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchSLAByModuleAsync(CancellationToken ct = default);
        Task EnsureSLAConfigsAsync(
            IEnumerable<(string Code, string Value, string Unit)> definitions,
            IEnumerable<(string LegacyCode, string CanonicalCode)> aliases,
            CancellationToken ct = default);
        Task UpdateSLAConfigAsync(string moduleCode, string value, string unit, CancellationToken ct = default);
        Task SetSLAConfigActiveAsync(string moduleCode, bool isActive, CancellationToken ct = default);
        Task<IEnumerable<Configuration>> FetchLatestConfigAsync(CancellationToken ct = default);
        Task SaveAttendanceParamsAsync(string configJsonValue, CancellationToken ct = default);
        Task<Configuration?> GetConfigByKeyAsync(string configGroup, string paramKey, CancellationToken ct = default);
        Task SaveConfigAsync(string configGroup, string paramKey, string value, string? description = null, bool isActive = true, CancellationToken ct = default);

        // ==========================================
        // 3. TEMPLATE REPO (Mẫu thông báo)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchAllTemplatesAsync(CancellationToken ct = default);
        Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson, CancellationToken ct = default);

        // ==========================================
        // 4. DOCUMENT EXPORT TEMPLATE REPO
        // ==========================================
        Task<IEnumerable<Configuration>> FetchDocumentTemplatesAsync(CancellationToken ct = default);
        Task SaveDocumentTemplateAsync(string templateKey, string templateJson, string? description = null, bool isActive = true, CancellationToken ct = default);
        Task EnsureDocumentTemplatesAsync(IEnumerable<(string TemplateKey, string TemplateJson, string Description, bool IsActive)> templates, CancellationToken ct = default);
    }
}
