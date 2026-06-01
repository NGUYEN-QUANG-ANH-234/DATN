namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class CorsConfiguration
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToArray();

        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            allowedOrigins = new[]
            {
                "http://localhost:5173",
                "https://localhost:5173"
            };
        }

        services.AddCors(options => {
            options.AddPolicy("AllowFrontend", policy => {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
        return services;
    }
}
