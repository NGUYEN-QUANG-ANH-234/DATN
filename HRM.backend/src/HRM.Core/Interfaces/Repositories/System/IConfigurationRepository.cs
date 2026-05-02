using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IConfigurationRepository : IBaseRepository<Configuration>
    {
        // ==========================================
        // 1. SALARY VARIABLE REPO (Biến lương)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchVariableMappingsAsync();
        Task SaveMappingAsync(string code, string tablePath);

        // ==========================================
        // 2. SYSTEM CONFIG REPO (SLA & Điểm danh)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchSLAByModuleAsync();
        Task UpdateSLAConfigAsync(string moduleCode, string value, string unit);
        Task<IEnumerable<Configuration>> FetchLatestConfigAsync();
        Task SaveAttendanceParamsAsync(string configJsonValue);

        // ==========================================
        // 3. TEMPLATE REPO (Mẫu thông báo)
        // ==========================================
        Task<IEnumerable<Configuration>> FetchAllTemplatesAsync();
        Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson);
    }
}
