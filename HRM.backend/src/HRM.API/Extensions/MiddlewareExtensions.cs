using HRM.backend.src.HRM.API.Middlewares;

namespace HRM.backend.src.HRM.API.Extensions;
public static class MiddlewareExtensions
{
    public static void UseCustomPipeline(this WebApplication app)
    {
        app.UseCors("AllowFrontend");

        app.Use((context, next) =>
        {
            //context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
            return next();
        });

        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDevTools();
        }
        
        app.UseHttpsRedirection();
        app.UseMiddleware<PerformanceMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }

    // Giữ nguyên hoặc chỉnh sửa hàm này làm helper nội bộ
    public static void UseDevTools(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(); // Tạo file JSON
            app.UseSwaggerUI(options => // Tạo giao diện web
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "HRM API v1");
                options.RoutePrefix = "swagger";
            });
            Console.WriteLine("--- Swagger UI is enabled at /swagger ---");
        }
    }
}