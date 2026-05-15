using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class CacheConfiguration
{
    public static IServiceCollection AddCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        // Lấy từ ConnectionStrings (đã map từ appsettings hoặc .env)
        var redisUrl = configuration.GetConnectionString("Redis");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisUrl;
            options.InstanceName = "HRMHICAS_Cache_";
        });

        // AppCache có thể là Singleton vì nó quản lý kết nối chung
        services.AddSingleton<IAppCache, RedisAppCache>();
        services.AddSingleton<ILockService, LockService>();

        return services;
    }
}