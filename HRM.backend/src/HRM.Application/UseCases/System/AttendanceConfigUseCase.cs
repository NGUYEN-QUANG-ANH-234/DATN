using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class AttendanceConfigUseCase : IAttendanceConfigUseCase
    {
        private readonly IConfigurationRepository _configRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;

        private const string CACHE_KEY = "Attendance_Config_Cache";

        public AttendanceConfigUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<AttendanceConfigDto?> GetConfigAsync(CancellationToken ct = default)
        {
            var cachedConfig = await _cache.GetAsync<AttendanceConfigDto>(CACHE_KEY);
            if (cachedConfig != null) return cachedConfig;

            var configs = await _configRepo.FetchLatestConfigAsync(ct);
            var configStr = configs.FirstOrDefault(c => c.ConfigGroup == "ATTENDANCE_PARAM")?.ParamValue;

            if (string.IsNullOrEmpty(configStr)) return null;

            var configDto = JsonSerializer.Deserialize<AttendanceConfigDto>(configStr);
            if (configDto != null)
            {
                await _cache.SetAsync(CACHE_KEY, configDto, TimeSpan.FromHours(24), null, ct);
            }

            return configDto;
        }

        public async Task<bool> UpdateConfigAsync(AttendanceConfigDto dto, int adminId, CancellationToken ct = default)
        {
            if (dto.Latitude < -90 || dto.Latitude > 90)
                throw new ArgumentException("Vĩ độ (Latitude) phải nằm trong khoảng -90 đến 90.");
            if (dto.Longitude < -180 || dto.Longitude > 180)
                throw new ArgumentException("Kinh độ (Longitude) phải nằm trong khoảng -180 đến 180.");
            if (dto.RadiusInMeters <= 0)
                throw new ArgumentException("Bán kính cho phép (m) phải lớn hơn 0.");

            var ipRegex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}(\/([0-9]|[1-2][0-9]|3[0-2]))?$");
            foreach (var ip in dto.AllowedIpRanges)
            {
                if (!ipRegex.IsMatch(ip))
                    throw new ArgumentException($"Định dạng IP/CIDR không hợp lệ: {ip}");
            }

            bool isSuccess = false;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var jsonContent = JsonSerializer.Serialize(dto);
                await _configRepo.SaveAttendanceParamsAsync(jsonContent, ct);

                // Ghi log đã được chuyển giao cho DbContext Hook lo liệu

                await _unitOfWork.CommitAsync(ct);
                isSuccess = true;
            }, ct);

            if (isSuccess) await _cache.RemoveAsync(CACHE_KEY, ct);

            return isSuccess;
        }
    }
}