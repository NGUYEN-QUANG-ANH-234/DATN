using Microsoft.OpenApi.Models;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "HRM HICAS API", Version = "v1" });

            // Định nghĩa nút "Authorize" trên giao diện
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Nhập token: Bearer {your_token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                new string[] { }
            }
        });
        });
        return services;
    }
}