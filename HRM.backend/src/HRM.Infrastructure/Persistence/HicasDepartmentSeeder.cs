using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.backend.src.HRM.Infrastructure.Persistence
{
    public static class HicasDepartmentSeeder
    {
        private sealed record DepartmentSeed(string Code, string Name, string? ParentCode);

        private static readonly DepartmentSeed[] DepartmentSeeds =
        {
            new("BOD", "Ban Giám đốc", null),
            new("HR", "Phòng Nhân sự", null),
            new("ACC", "Phòng Tài chính - Kế toán", null),
            new("SALE", "Phòng Kinh doanh và Marketing", null),
            new("PMO", "Phòng Quản lý dự án", null),
            new("TECH", "Khối Kỹ thuật phần mềm", null),
            new("PRODUCT", "Phòng Sản phẩm", null),
            new("IMPL", "Phòng Triển khai và Hỗ trợ khách hàng", null),

            new("CADBIM", "Bộ phận CAD/BIM", "TECH"),
            new("ERP", "Bộ phận ERP và Giải pháp doanh nghiệp", "TECH"),
            new("AIDATA", "Bộ phận AI và Dữ liệu", "TECH"),
            new("WEBMOB", "Bộ phận Web và Mobile", "TECH"),
            new("QA", "Bộ phận QA/QC", "TECH"),
            new("RND", "Trung tâm R&D", "TECH"),

            new("VITHEP", "Nhóm sản phẩm ViTHEP", "PRODUCT"),
            new("ANYON", "Nhóm sản phẩm AnyOn", "PRODUCT"),
            new("SMARTMTO", "Nhóm sản phẩm SmartMTO", "PRODUCT"),

            new("AECIMPL", "Nhóm triển khai AEC/BIM", "IMPL"),
            new("ERPIMPL", "Nhóm triển khai ERP", "IMPL"),
            new("HCMREP", "Văn phòng đại diện TP.HCM", "IMPL")
        };

        private static readonly Dictionary<string, string[]> LegacyDepartmentNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ACC"] = new[] { "Phòng Kế toán" },
            ["SALE"] = new[] { "Phòng Kinh doanh" },
            ["TECH"] = new[] { "Phòng Kỹ thuật" }
        };

        public static async Task<IReadOnlyList<Department>> SyncAsync(MyDbContext context, CancellationToken ct = default)
        {
            var seedCodes = DepartmentSeeds.Select(d => d.Code).ToList();
            var existingDepartments = await context.Departments
                .Where(d => seedCodes.Contains(d.DeptCode))
                .ToDictionaryAsync(d => d.DeptCode, ct);

            var hasChanges = false;

            foreach (var seed in DepartmentSeeds)
            {
                if (!existingDepartments.TryGetValue(seed.Code, out var department))
                {
                    department = new Department
                    {
                        DeptCode = seed.Code,
                        DeptName = seed.Name,
                        Status = DeptStatus.Active
                    };

                    context.Departments.Add(department);
                    existingDepartments[seed.Code] = department;
                    hasChanges = true;
                    continue;
                }

                if (department.DeptName != seed.Name && ShouldUpdateName(seed.Code, department.DeptName))
                {
                    department.DeptName = seed.Name;
                    hasChanges = true;
                }
            }

            foreach (var seed in DepartmentSeeds)
            {
                if (seed.ParentCode is null) continue;

                var department = existingDepartments[seed.Code];
                var parentDepartment = existingDepartments[seed.ParentCode];

                if (department.ParentDeptId is null && department.ParentDepartment is null)
                {
                    department.ParentDepartment = parentDepartment;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync(ct);
            }

            return DepartmentSeeds.Select(seed => existingDepartments[seed.Code]).ToList();
        }

        public static async Task AutoSyncAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            await SyncAsync(context, ct);
        }

        private static bool ShouldUpdateName(string code, string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName)) return true;

            return LegacyDepartmentNames.TryGetValue(code, out var names)
                && names.Contains(currentName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
