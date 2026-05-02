using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class CacheConfiguration
{
    public static IServiceCollection AddCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "DemoWebAPI_";
        });

        services.AddSingleton<IAppCache, RedisAppCache>();
        services.AddSingleton<ILockService, LockService>();

        return services;
    }
}