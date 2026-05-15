using HRM.backend.src.HRM.API.Extensions;
using HRM.backend.src.HRM.Infrastructure.Configurations;
using Serilog;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace HRM.backend;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Khởi tạo cấu hình hệ thống ban đầu
        SetupPreBuildConfiguration();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // <-- THÊM DÒNG NÀY: Ép hệ thống ưu tiên đọc User Secrets 
            builder.Configuration.AddUserSecrets<Program>();

            builder.Configuration.AddEnvironmentVariables();

            // 2. Cấu hình Logging (Infrastructure)
            builder.Host.UseSerilog();

            // 3. Đăng ký các dịch vụ (Dependency Injection)
            ConfigureServices(builder);

            var app = builder.Build();

            //Thêm đoạn này để kích hoạt Seed dữ liệu
            //using (var scope = app.Services.CreateScope())
            //{
            //    var services = scope.ServiceProvider;
            //    var context = services.GetRequiredService<MyDbContext>();
            //    await DbInitializer.SeedData(context); // Gọi hàm Seed tại đây
            //}

            // 4. Cấu hình HTTP Request Pipeline (Middleware)
            app.UseCustomPipeline();

            await PermissionSeederExtension.AutoSyncPermissionsAsync(app.Services);

            Log.Information(" HRM HICAS System is running...");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            HandleStartupException(ex);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void SetupPreBuildConfiguration()
    {
        DotNetEnv.Env.Load();
        Console.OutputEncoding = Encoding.UTF8;

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services
            .AddHttpContextAccessor()
            .SetupInfrastructureLogging(builder.Configuration)
            .AddDatabaseConfig(builder.Configuration)
            .AddRepositoriesConfig()                // Data Access Layers
            .AddServicesConfig(builder.Configuration)                    // Business Logic & External Services (MỚI THÊM)
            .AddCacheConfig(builder.Configuration)                       // Redis & Caching
            .AddSecurityConfig(builder.Configuration) // JWT & AuthService
            .AddCustomAuthorization()               // Phân quyền động (Dynamic RBAC)
            .AddCustomCors()                        // CORS
            .AddCustomControllers()                 // Controllers & JSON
            .AddSwaggerConfig();                    // Swagger UI
    }
    

    private static void HandleStartupException(Exception ex)
    {
        Log.Fatal(ex, "LỖI CHÍ MẠNG!");
        Console.WriteLine("=================================================");
        Console.WriteLine("LỖI CHI TIẾT TÌM THẤY:");

        Console.WriteLine(ex.ToString());

        Console.WriteLine("=================================================");
        Console.ReadKey();
    }
}