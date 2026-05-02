using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
    {
        public ConfigurationRepository(MyDbContext context) : base(context) { }

        // --- Helper dùng chung để Upsert (Cập nhật hoặc Thêm mới) ---
        private async Task UpsertConfigAsync(string key, string value, string? description = null)
        {
            var config = await _dbSet.FirstOrDefaultAsync(c => c.ParamKey == key);
            if (config != null)
            {
                config.ParamValue = value;
                if (description != null) config.Description = description;
                _dbSet.Update(config);
            }
            else
            {
                await _dbSet.AddAsync(new Configuration { ParamKey = key, ParamValue = value, Description = description });
            }
        }

        // ==========================================
        // 1. SALARY VARIABLE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchVariableMappingsAsync()
        {
            return await _dbSet.Where(c => c.ParamKey.StartsWith("SALARY_VAR_")).ToListAsync();
        }

        public async Task SaveMappingAsync(string code, string tablePath)
        {
            await UpsertConfigAsync($"SALARY_VAR_{code.ToUpper()}", tablePath, "Biến lương động");
        }

        // ==========================================
        // 2. SYSTEM CONFIG (SLA & Attendance)
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchSLAByModuleAsync()
        {
            return await _dbSet.Where(c => c.ParamKey.StartsWith("SLA_")).ToListAsync();
        }

        public async Task UpdateSLAConfigAsync(string moduleCode, string value, string unit)
        {
            await UpsertConfigAsync($"SLA_{moduleCode.ToUpper()}", value, $"Unit: {unit}");
        }

        public async Task<IEnumerable<Configuration>> FetchLatestConfigAsync()
        {
            // Bỏ qua các template và biến lương, chỉ lấy config hệ thống
            return await _dbSet.Where(c => !c.ParamKey.StartsWith("TEMPLATE_") && !c.ParamKey.StartsWith("SALARY_VAR_")).ToListAsync();
        }

        public async Task SaveAttendanceParamsAsync(string configJsonValue)
        {
            await UpsertConfigAsync("ATTENDANCE_CONFIG", configJsonValue, "Cấu hình chấm công (JSON)");
        }

        // ==========================================
        // 3. TEMPLATE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchAllTemplatesAsync()
        {
            return await _dbSet.Where(c => c.ParamKey.StartsWith("TEMPLATE_")).ToListAsync();
        }

        public async Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson)
        {
            await UpsertConfigAsync(templateKey.StartsWith("TEMPLATE_") ? templateKey : $"TEMPLATE_{templateKey.ToUpper()}", subjectAndBodyJson, "Mẫu email/thông báo");
        }
    }
}
