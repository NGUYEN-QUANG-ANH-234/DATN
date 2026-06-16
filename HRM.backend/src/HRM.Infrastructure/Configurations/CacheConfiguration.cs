using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;
using StackExchange.Redis;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class CacheConfiguration
{
    public static IServiceCollection AddCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var redisUrl = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IAppCache, RedisAppCache>();
            services.AddSingleton<ILockService, LockService>();
            return services;
        }

        var redisOptions = ConfigurationOptions.Parse(redisUrl);
        redisOptions.AbortOnConnectFail = false;

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
            options.InstanceName = "HRMHICAS_Cache_";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
        services.AddSingleton<IAppCache, RedisAppCache>();
        services.AddSingleton<ILockService, RedisDistributedLockService>();

        return services;
    }
}
