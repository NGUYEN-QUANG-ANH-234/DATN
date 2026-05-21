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

            var controllers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t));

            // 1. Quét và gom nhóm, lấy định nghĩa chuẩn nhất từ code
            var apiPermissions = controllers
                .SelectMany(t => t.GetCustomAttributes<RequirePermissionAttribute>())
                .Concat(
                    controllers.SelectMany(t => t.GetMethods())
                                .SelectMany(m => m.GetCustomAttributes<RequirePermissionAttribute>())
                )
                .GroupBy(a => a.PermissionCode)
                .Select(g => g.OrderByDescending(a => a.GroupName != "Chưa phân loại").First())
                .ToList();

            if (!apiPermissions.Any()) return;

            // 2. Lấy toàn bộ Entity quyền trong DB (để kiểm tra và Update)
            var dbPermissions = await context.Set<Permission>().ToListAsync();
            var hasChanges = false;

            // 3. Xử lý Upsert (Thêm mới hoặc Cập nhật)
            foreach (var apiPerm in apiPermissions)
            {
                var existingPerm = dbPermissions.FirstOrDefault(p => p.PermissionCode == apiPerm.PermissionCode);

                if (existingPerm == null)
                {
                    // Chưa có -> Thêm mới
                    context.Set<Permission>().Add(new Permission
                    {
                        PermissionCode = apiPerm.PermissionCode,
                        GroupName = apiPerm.GroupName,
                        Description = apiPerm.Description
                    });
                    hasChanges = true;
                }
                else
                {
                    // Đã có -> Kiểm tra xem có cần cập nhật GroupName/Description không
                    if (existingPerm.GroupName != apiPerm.GroupName || existingPerm.Description != apiPerm.Description)
                    {
                        existingPerm.GroupName = apiPerm.GroupName;
                        existingPerm.Description = apiPerm.Description;
                        context.Set<Permission>().Update(existingPerm);
                        hasChanges = true;
                    }
                }
            }

            // 4. Chỉ gọi SaveChanges khi thực sự có thay đổi
            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }        
    }
}
