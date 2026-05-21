using System.Text.Json;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Models.History;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile
{
    public class HistoryTrackingRepository : IHistoryTrackingRepository
    {
        private readonly MyDbContext _context;

        public HistoryTrackingRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ConsolidatedHistoryRecord>> GetPagedConsolidatedHistoryAsync(
            int employeeId,
            HistoryFilterCriteria filter,
            CancellationToken ct = default)
        {
            var normalizedType = NormalizeType(filter.Type);
            var events = new List<ConsolidatedHistoryRecord>();

            if (ShouldInclude(normalizedType, "EMPLOYMENT"))
                events.AddRange(await BuildEmploymentEventsAsync(employeeId, filter.Year, ct));

            if (ShouldInclude(normalizedType, "PROFILE"))
                events.AddRange(await BuildProfileEventsAsync(employeeId, filter.Year, ct));

            if (ShouldInclude(normalizedType, "CONTRACT"))
                events.AddRange(await BuildContractEventsAsync(employeeId, filter.Year, ct));

            if (ShouldInclude(normalizedType, "ADDENDUM"))
                events.AddRange(await BuildAddendumEventsAsync(employeeId, filter.Year, ct));

            var ordered = events
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.RefId ?? 0)
                .ToList();

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var size = filter.Size <= 0 ? 10 : Math.Min(filter.Size, 50);

            return new PagedResult<ConsolidatedHistoryRecord>
            {
                Items = ordered.Skip((page - 1) * size).Take(size).ToList(),
                TotalCount = ordered.Count,
                Page = page,
                Size = size
            };
        }

        private async Task<List<ConsolidatedHistoryRecord>> BuildEmploymentEventsAsync(int employeeId, int? year, CancellationToken ct)
        {
            var histories = await _context.EmploymentHistories
                .Where(h => h.EmployeeId == employeeId)
                .AsNoTracking()
                .ToListAsync(ct);

            return histories
                .Where(h => !year.HasValue || h.EffectiveDate.Year == year.Value || h.ChangeDate.Year == year.Value)
                .Select(h => new ConsolidatedHistoryRecord
                {
                    Date = h.EffectiveDate == default ? h.ChangeDate : h.EffectiveDate,
                    EventType = "EMPLOYMENT",
                    Title = GetEmploymentTitle(h.Type),
                    Description = BuildValueDescription(h.OldValue, h.NewValue),
                    RefId = h.Id,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue
                })
                .ToList();
        }

        private async Task<List<ConsolidatedHistoryRecord>> BuildProfileEventsAsync(int employeeId, int? year, CancellationToken ct)
        {
            var requests = await _context.ProfileUpdateRequests
                .Where(r => r.EmployeeId == employeeId && r.Status == RequestStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);

            return requests
                .Where(r => !year.HasValue || r.CreatedAt.Year == year.Value)
                .Select(r => new ConsolidatedHistoryRecord
                {
                    Date = r.CreatedAt,
                    EventType = "PROFILE",
                    Title = "Cập nhật hồ sơ cá nhân",
                    Description = BuildProfileDescription(r.RequestedDataJson),
                    RefId = r.Id,
                    NewValue = r.RequestedDataJson
                })
                .ToList();
        }

        private async Task<List<ConsolidatedHistoryRecord>> BuildContractEventsAsync(int employeeId, int? year, CancellationToken ct)
        {
            var contracts = await _context.Contracts
                .Where(c => c.EmployeeId == employeeId && c.Status == ContractStatus.Active)
                .AsNoTracking()
                .ToListAsync(ct);

            return contracts
                .Where(c => !year.HasValue || c.StartDate.Year == year.Value)
                .Select(c => new ConsolidatedHistoryRecord
                {
                    Date = c.StartDate,
                    EventType = "CONTRACT",
                    Title = $"Hợp đồng {c.ContractNumber} có hiệu lực",
                    Description = $"Loại {c.ContractType}, lương cơ bản {FormatMoney(c.BasicSalary)}, thời hạn {FormatDate(c.StartDate)} - {FormatDate(c.EndDate)}.",
                    RefId = c.Id,
                    NewValue = c.ContractNumber
                })
                .ToList();
        }

        private async Task<List<ConsolidatedHistoryRecord>> BuildAddendumEventsAsync(int employeeId, int? year, CancellationToken ct)
        {
            var addendums = await _context.ContractAddendums
                .Include(a => a.Contract)
                .Where(a => a.Contract != null && a.Contract.EmployeeId == employeeId && a.Status == AddendumStatus.Active)
                .AsNoTracking()
                .ToListAsync(ct);

            return addendums
                .Where(a => !year.HasValue || a.EffectiveDate.Year == year.Value)
                .Select(a => new ConsolidatedHistoryRecord
                {
                    Date = a.EffectiveDate,
                    EventType = "ADDENDUM",
                    Title = $"Phụ lục {a.AddendumNumber} có hiệu lực",
                    Description = BuildAddendumDescription(a.Content, a.NewBasicSalary, a.NewInsuranceSalary, a.NewEndDate, a.OtherChangesJson),
                    RefId = a.Id,
                    NewValue = a.AddendumNumber
                })
                .ToList();
        }

        private static string NormalizeType(string? type)
        {
            return string.IsNullOrWhiteSpace(type) ? "ALL" : type.Trim().ToUpperInvariant();
        }

        private static bool ShouldInclude(string selectedType, string eventType)
        {
            return selectedType == "ALL" || selectedType == eventType;
        }

        private static string GetEmploymentTitle(HistoryType type) => type switch
        {
            HistoryType.Onboarding => "Tiếp nhận nhân sự",
            HistoryType.Promotion => "Thăng chức",
            HistoryType.Appointment => "Bổ nhiệm hoặc điều chỉnh điều khoản",
            HistoryType.Transfer => "Điều chuyển phòng ban",
            HistoryType.Salary_Change => "Điều chỉnh lương",
            HistoryType.Disciplinary => "Kỷ luật",
            HistoryType.Termination => "Chấm dứt hợp đồng",
            _ => "Biến động nhân sự"
        };

        private static string BuildValueDescription(string? oldValue, string? newValue)
        {
            if (string.IsNullOrWhiteSpace(oldValue) && string.IsNullOrWhiteSpace(newValue))
                return "Ghi nhận biến động nhân sự.";

            if (string.IsNullOrWhiteSpace(oldValue))
                return $"Giá trị mới: {newValue}";

            if (string.IsNullOrWhiteSpace(newValue))
                return $"Giá trị cũ: {oldValue}";

            return $"Từ {oldValue} sang {newValue}.";
        }

        private static string BuildProfileDescription(string json)
        {
            var fields = ExtractJsonKeys(json);
            if (fields.Count == 0)
                return "Hồ sơ cá nhân đã được HR phê duyệt cập nhật.";

            return $"Cập nhật {fields.Count} trường: {string.Join(", ", fields.Take(6))}{(fields.Count > 6 ? "..." : ".")}";
        }

        private static string BuildAddendumDescription(
            string? content,
            decimal? newBasicSalary,
            decimal? newInsuranceSalary,
            DateTime? newEndDate,
            string? otherChangesJson)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(content)) parts.Add(content);
            if (newBasicSalary.HasValue) parts.Add($"Lương cơ bản mới {FormatMoney(newBasicSalary.Value)}");
            if (newInsuranceSalary.HasValue) parts.Add($"Lương BHXH mới {FormatMoney(newInsuranceSalary.Value)}");
            if (newEndDate.HasValue) parts.Add($"Ngày kết thúc mới {FormatDate(newEndDate)}");

            var otherFields = ExtractJsonKeys(otherChangesJson);
            if (otherFields.Count > 0) parts.Add($"Thay đổi khác: {string.Join(", ", otherFields)}");

            return parts.Count == 0 ? "Phụ lục hợp đồng đã được phê duyệt và có hiệu lực." : string.Join("; ", parts) + ".";
        }

        private static List<string> ExtractJsonKeys(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new List<string>();

                return doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        private static string FormatMoney(decimal value)
        {
            return string.Format("{0:N0} VND", value);
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "không xác định";
        }
    }
}
