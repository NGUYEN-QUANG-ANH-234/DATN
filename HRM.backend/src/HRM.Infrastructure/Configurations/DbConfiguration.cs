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

           // 1. Thay Serilog bằng Console.WriteLine để in ngay lập tức không qua bộ đệm
           .LogTo(message =>
           {
               // Đổi màu để dễ nhìn thấy thảm họa "cuộn" màn hình
               Console.ForegroundColor = ConsoleColor.Yellow;
               Console.WriteLine(message);
               Console.ResetColor();
           },
               new[] { DbLoggerCategory.Database.Command.Name },
               LogLevel.Information)

           // 2. Bật Sensitive Data Logging để thấy đích danh ID nào đang bị lặp vô tận
           .EnableSensitiveDataLogging()

            //.UseLoggerFactory(MyDbContext.MyLoggerFactory) // Tạm thời comment dòng này lại
           .EnableDetailedErrors());
           //.UseLazyLoadingProxies()); // Thủ phạm gây lặp đang nằm ở đây


        return services;
    }
}