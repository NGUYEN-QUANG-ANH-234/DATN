using Microsoft.AspNetCore.Authorization;
using HRM.backend.src.HRM.API.Authorization;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();

        // 1. Đăng ký Provider xử lý Policy động (Singular - Không cần DB ở đây)
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // 2. Đăng ký Handler kiểm tra quyền thực tế
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        return services;
    }
}