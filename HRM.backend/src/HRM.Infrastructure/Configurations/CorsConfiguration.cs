namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class CorsConfiguration
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options => {
            options.AddPolicy("AllowFrontend", policy => {
                policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
        return services;
    }
}