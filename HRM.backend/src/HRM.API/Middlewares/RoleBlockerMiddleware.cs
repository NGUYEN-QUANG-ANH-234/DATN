using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using System.Security.Claims;
using System.Text.Json;

namespace HRM.backend.src.HRM.API.Middlewares
{
    public class RoleBlockerMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleBlockerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var requiredPermission = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>()?.PermissionCode;

            if (!string.IsNullOrEmpty(requiredPermission))
            {
                var accountIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleIdClaim = context.User.FindFirst("RoleId")?.Value;

                if (!int.TryParse(accountIdClaim, out var accountId) ||
                    !int.TryParse(roleIdClaim, out var roleId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var accountRepo = context.RequestServices.GetRequiredService<IAccountRepository>();
                var account = await accountRepo.GetByIdAsync(accountId, context.RequestAborted);
                if (account == null || account.Status != AccountStatus.Active)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "Tài khoản đã bị khóa hoặc ngừng hoạt động."
                    }));
                    return;
                }

                if (roleId != 1)
                {
                    var rbacUseCase = context.RequestServices.GetRequiredService<IRbacUseCase>();
                    var matrix = await rbacUseCase.GetAllRolesAndPermissionsAsync();
                    var currentRole = matrix.FirstOrDefault(r => r.RoleId == roleId);

                    if (currentRole == null || !currentRole.Permissions.Contains(requiredPermission))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = $"Truy cập bị từ chối. Bạn thiếu quyền: [{requiredPermission}]"
                        }));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequirePermissionAttribute : Attribute
    {
        public string PermissionCode { get; }
        public string GroupName { get; set; }
        public string Description { get; set; }

        public RequirePermissionAttribute(string permissionCode)
        {
            PermissionCode = permissionCode;
            GroupName = "Chưa phân loại";
            Description = "Hệ thống tự động quét từ mã nguồn";
        }
    }
}
