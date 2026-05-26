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
        private readonly ILockService _lockService;

        private const string CACHE_KEY = "Attendance_Config_Cache";

        public AttendanceConfigUseCase(
            IConfigurationRepository configRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService)
        {
            _configRepo = configRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<AttendanceConfigDto?> GetConfigAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrSetWithLockAsync(
                CACHE_KEY,
                async (innerCt) =>
                {
                    var configs = await _configRepo.FetchLatestConfigAsync(innerCt);
                    var configStr = configs.FirstOrDefault(c => c.ConfigGroup == "ATTENDANCE_PARAM")?.ParamValue;

                    if (string.IsNullOrEmpty(configStr)) return null;

                    var configDto = JsonSerializer.Deserialize<AttendanceConfigDto>(configStr);
                    if (configDto != null)
                        NormalizeOfficeLocations(configDto);

                    return configDto;
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<bool> UpdateConfigAsync(AttendanceConfigDto dto, int adminId, CancellationToken ct = default)
        {
            NormalizeOfficeLocations(dto);
            ValidateConfig(dto);

            bool isSuccess = false;

            await _lockService.GetWithLockAsync("attendance_config", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var jsonContent = JsonSerializer.Serialize(dto);
                    await _configRepo.SaveAttendanceParamsAsync(jsonContent, innerCt);

                    await _unitOfWork.CommitAsync(innerCt);
                    isSuccess = true;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (isSuccess) await _cache.RemoveAsync(CACHE_KEY, ct);

            return isSuccess;
        }

        private static void NormalizeOfficeLocations(AttendanceConfigDto dto)
        {
            dto.AllowedIpRanges ??= new List<string>();
            dto.OfficeLocations ??= new List<AttendanceOfficeLocationDto>();

            if (dto.OfficeLocations.Count == 0 &&
                (dto.Latitude != 0 || dto.Longitude != 0 || dto.RadiusInMeters > 0 || dto.AllowedIpRanges.Count > 0))
            {
                dto.OfficeLocations.Add(new AttendanceOfficeLocationDto
                {
                    Name = "Cơ sở chính",
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    RadiusInMeters = dto.RadiusInMeters,
                    AllowedIpRanges = dto.AllowedIpRanges.ToList(),
                    IsActive = true
                });
            }

            var firstActive = dto.OfficeLocations.FirstOrDefault(x => x.IsActive) ?? dto.OfficeLocations.FirstOrDefault();
            if (firstActive != null)
            {
                dto.Latitude = firstActive.Latitude;
                dto.Longitude = firstActive.Longitude;
                dto.RadiusInMeters = firstActive.RadiusInMeters;
                dto.AllowedIpRanges = firstActive.AllowedIpRanges.ToList();
            }
        }

        private static void ValidateConfig(AttendanceConfigDto dto)
        {
            if (dto.OfficeLocations.Count == 0)
                throw new ArgumentException("Cần cấu hình ít nhất một cơ sở chấm công.");

            var ipRegex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}(\/([0-9]|[1-2][0-9]|3[0-2]))?$");

            foreach (var office in dto.OfficeLocations)
            {
                if (string.IsNullOrWhiteSpace(office.Name))
                    throw new ArgumentException("Tên cơ sở chấm công không được để trống.");
                if (office.Latitude < -90 || office.Latitude > 90)
                    throw new ArgumentException($"Vĩ độ của {office.Name} phải nằm trong khoảng -90 đến 90.");
                if (office.Longitude < -180 || office.Longitude > 180)
                    throw new ArgumentException($"Kinh độ của {office.Name} phải nằm trong khoảng -180 đến 180.");
                if (office.RadiusInMeters <= 0)
                    throw new ArgumentException($"Bán kính của {office.Name} phải lớn hơn 0.");
                if (office.AllowedIpRanges.Count == 0)
                    throw new ArgumentException($"{office.Name} cần có ít nhất một IP/CIDR hợp lệ.");

                foreach (var ip in office.AllowedIpRanges)
                {
                    if (!ipRegex.IsMatch(ip))
                        throw new ArgumentException($"Định dạng IP/CIDR không hợp lệ ở {office.Name}: {ip}");
                }
            }
        }
    }
}
