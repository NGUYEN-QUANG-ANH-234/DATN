using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
    {
        public ConfigurationRepository(MyDbContext context) : base(context) { }

        // --- Helper dùng chung để Upsert (Cập nhật hoặc Thêm mới) ---
        // BỔ SUNG: Tham số configGroup
        private async Task UpsertConfigAsync(string configGroup, string key, string value, string? description = null, CancellationToken ct = default)
        {
            var config = await _dbSet.FirstOrDefaultAsync(c => c.ParamKey == key, ct);

            if (config != null)
            {
                config.ParamValue = value;
                config.ConfigGroup = configGroup; // Đảm bảo đồng bộ Group
                if (description != null) config.Description = description;

                _dbSet.Update(config);
            }
            else
            {
                await _dbSet.AddAsync(new Configuration
                {
                    ConfigGroup = configGroup,
                    ParamKey = key,
                    ParamValue = value,
                    Description = description
                }, ct);
            }
        }

        // ==========================================
        // 1. SALARY VARIABLE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchVariableMappingsAsync(CancellationToken ct = default)
        {
            // Thay vì quét chuỗi StartsWith, chỉ cần lọc chính xác theo Group
            return await _dbSet.Where(c => c.ConfigGroup == "SALARY_VARIABLE").ToListAsync(ct);
        }

        public async Task SaveMappingAsync(string code, string tablePath, CancellationToken ct = default)
        {
            await UpsertConfigAsync("SALARY_VARIABLE", $"SALARY_VAR_{code.ToUpper()}", tablePath, "Biến lương động", ct);
        }

        // ==========================================
        // 2. SYSTEM CONFIG (SLA & Attendance)
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchSLAByModuleAsync(CancellationToken ct = default)
        {
            return await _dbSet.Where(c => c.ConfigGroup == "SLA_TIME").ToListAsync(ct);
        }

        public async Task UpdateSLAConfigAsync(string moduleCode, string value, string unit, CancellationToken ct = default)
        {
            await UpsertConfigAsync("SLA_TIME", $"SLA_{moduleCode.ToUpper()}", value, $"Unit: {unit}", ct);
        }

        public async Task<IEnumerable<Configuration>> FetchLatestConfigAsync(CancellationToken ct = default)
        {
            // Lấy chính xác các nhóm liên quan đến cấu hình vận hành
            return await _dbSet.Where(c => c.ConfigGroup == "SLA_TIME" || c.ConfigGroup == "ATTENDANCE_PARAM").ToListAsync(ct);
        }

        public async Task SaveAttendanceParamsAsync(string configJsonValue, CancellationToken ct = default)
        {
            await UpsertConfigAsync("ATTENDANCE_PARAM", "ATTENDANCE_CONFIG", configJsonValue, "Cấu hình chấm công (JSON)", ct);
        }

        // ==========================================
        // 3. TEMPLATE
        // ==========================================
        public async Task<IEnumerable<Configuration>> FetchAllTemplatesAsync(CancellationToken ct = default)
        {
            return await _dbSet.Where(c => c.ConfigGroup == "MAIL_TEMPLATE").ToListAsync(ct);
        }

        public async Task UpdateTemplateContentAsync(string templateKey, string subjectAndBodyJson, CancellationToken ct = default)
        {
            var key = templateKey.StartsWith("TEMPLATE_") ? templateKey : $"TEMPLATE_{templateKey.ToUpper()}";
            await UpsertConfigAsync("MAIL_TEMPLATE", key, subjectAndBodyJson, "Mẫu email/thông báo", ct);
        }
    }
}