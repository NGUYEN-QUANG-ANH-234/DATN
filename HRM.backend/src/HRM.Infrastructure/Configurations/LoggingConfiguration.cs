using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // Thêm namespace này
using Serilog;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class LoggingConfiguration
{
    // Đổi void -> IServiceCollection và thêm "this IServiceCollection services"
    public static IServiceCollection SetupInfrastructureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        // Trả về services để có thể dùng dấu chấm (.) gọi hàm tiếp theo
        return services;
    }
}