//using DemoWebAPI.Application.Services;
using HRM.backend.src.HRM.API.Extensions;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Services;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class AuthConfiguration
{
    public static IServiceCollection AddSecurityConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IJwtService, JwtService>();

        // Giả định phương thức AddJwtAuthentication của bạn đã được viết sẵn ở đâu đó
        services.AddJwtAuthentication(configuration);
        

        services.AddHttpContextAccessor();

        return services;
    }
}