using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HRM.backend.src.HRM.Infrastructure.Configurations;

public static class DbConfiguration
{
    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        DotNetEnv.Env.Load();

        var connString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connString))
        {
            Log.Error("DATABASE_ERROR: Connection string is null!");
            throw new InvalidOperationException("Lỗi: Không tìm thấy DefaultConnection!");
        }

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
           .EnableDetailedErrors()
           .UseLazyLoadingProxies()); // Thủ phạm gây lặp đang nằm ở đây


        return services;
    }
}