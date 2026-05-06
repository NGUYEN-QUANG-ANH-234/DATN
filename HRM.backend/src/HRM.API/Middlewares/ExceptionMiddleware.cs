using Serilog;
using System.Net;
using System.Text.Json;

namespace HRM.backend.src.HRM.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger; // Nên dùng Logger thay vì Console.WriteLine

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // 1. Log lỗi để kiểm tra trong cửa sổ Output hoặc file Log
                // ĐÚNG & AN TOÀN TUYỆT ĐỐI:
                _logger.LogError("Lỗi tại request: {Path}. Chi tiết: {Message}", context.Request.Path, ex.InnerException?.Message ?? ex.Message);

                // 2. Cấu hình Response (Phải làm trước khi ghi Body)
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // 500

                // 3. Chỉ trả về chi tiết lỗi khi đang ở môi trường Development
                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Có lỗi xảy ra từ phía Server!",
                    Detailed = ex.Message // Chỉ nên trả về ex.StackTrace nếu bạn đang cần debug gấp
                };

                // 4. Ghi dữ liệu ra JSON (Chỉ gọi duy nhất 1 lần)
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, jsonOptions);

                await context.Response.WriteAsync(json);
            }
        }
    }
}