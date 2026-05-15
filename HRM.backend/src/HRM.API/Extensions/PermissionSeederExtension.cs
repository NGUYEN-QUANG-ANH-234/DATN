using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HRM.backend.src.HRM.API.Extensions
{
    public class PermissionSeederExtension
    {
        public static async Task AutoSyncPermissionsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            // 1. Quét toàn bộ Controller để tìm các mã quyền từ RequirePermissionAttribute
            var apiPermissions = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t)) // Lấy các Controller
                .SelectMany(t => t.GetMethods()) // Lấy các API Action
                .SelectMany(m => m.GetCustomAttributes<RequirePermissionAttribute>())
                .Select(a => a.PermissionCode)
                .Distinct()
                .ToList();

            if (!apiPermissions.Any()) return;

            // 2. Lấy các quyền hiện có trong DB
            var dbPermissions = await context.Set<Permission>()
                .Select(p => p.PermissionCode)
                .ToListAsync();

            // 3. Tìm các quyền có trong Code nhưng chưa có trong DB (Delta)
            var newPermissions = apiPermissions
                .Except(dbPermissions)
                .Select(code => new Permission
                {
                    PermissionCode = code,
                    GroupName = "Chưa phân loại", // Admin sẽ vào UI để đổi tên nhóm sau
                    Description = "Hệ thống tự động quét từ mã nguồn"
                }).ToList();

            // 4. Lưu vào DB nếu có quyền mới
            if (newPermissions.Any())
            {
                await context.Set<Permission>().AddRangeAsync(newPermissions);
                await context.SaveChangesAsync();
            }
        }
    }
}
