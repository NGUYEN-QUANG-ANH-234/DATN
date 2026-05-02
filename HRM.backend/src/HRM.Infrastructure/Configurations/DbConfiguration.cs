using HRM.backend.src.HRM.Core.Interfaces;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class DbConfiguration
{
    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services)
    {
        DotNetEnv.Env.Load();

        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var database = Environment.GetEnvironmentVariable("DB_SERVER_NAME");

        // Dòng này để "bắt ma" khi chạy lệnh Migration
        Console.WriteLine($"--- DEBUG MIGRATION: Server='{server}', DB='{database}' ---");

        var connString = $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
                         $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                         $"Database={Environment.GetEnvironmentVariable("DB_SERVER_NAME")};" +
                         $"Uid={Environment.GetEnvironmentVariable("DB_USER")};" +
                         $"Pwd={Environment.GetEnvironmentVariable("DB_PWD")};" +
                         "SslMode=None;AllowPublicKeyRetrieval=True;Max Pool Size=1000;";

        services.AddDbContext<MyDbContext>(options =>
            options.UseMySql(connString, ServerVersion.AutoDetect(connString),
                mySqlOptions => { mySqlOptions.EnableRetryOnFailure(); })
                   .LogTo(message => Log.Information(message), new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                   .UseLoggerFactory(MyDbContext.MyLoggerFactory)
                   .EnableDetailedErrors()
                   .UseLazyLoadingProxies());


        return services;
    }
}