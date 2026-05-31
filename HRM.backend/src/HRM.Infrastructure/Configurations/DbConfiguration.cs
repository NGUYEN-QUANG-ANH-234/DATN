using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Serilog;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class DbConfiguration
{
    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        DotNetEnv.Env.Load();

        var connString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connString))
        {
            Log.Error("DATABASE_ERROR: Connection string is null.");
            throw new InvalidOperationException("Không tìm thấy DefaultConnection.");
        }

        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);
        var enableSensitiveLogging = configuration.GetValue<bool>("EfCore:EnableSensitiveDataLogging");
        var enableDetailedErrors = configuration.GetValue("EfCore:EnableDetailedErrors", isDevelopment);

        services.AddDbContext<MyDbContext>(options =>
        {
            options.UseMySql(
                    connString,
                    ServerVersion.AutoDetect(connString),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure();
                        mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                .UseLazyLoadingProxies();

            if (isDevelopment)
            {
                options.LogTo(message => Log.Debug("{EfCoreMessage}", message),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);
            }

            if (enableSensitiveLogging)
                options.EnableSensitiveDataLogging();

            if (enableDetailedErrors)
                options.EnableDetailedErrors();
        });

        return services;
    }
}
