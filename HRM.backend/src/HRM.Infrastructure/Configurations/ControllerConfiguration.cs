using System.Text.Json.Serialization;
using System.Text.Json;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class ControllerConfiguration
{
    public static IServiceCollection AddCustomControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        return services;
    }
}