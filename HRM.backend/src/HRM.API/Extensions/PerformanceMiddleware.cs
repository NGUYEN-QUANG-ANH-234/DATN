using System.Diagnostics;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    // 1. Khách mang theo 'context' đi vào cửa này
    {
        // TRƯỚC khi gọi next: Ta chuẩn bị đồng hồ bấm giờ
        var sw = Stopwatch.StartNew();

        // 2. Gọi 'next': Đẩy khách đi tiếp vào các Middleware khác và vào Controller
        await _next(context);

        // SAU khi gọi next: Lúc này Controller đã xử lý xong và đang quay trở ra
        sw.Stop(); 

        // 3. Dùng 'ILogger' để ghi lại kết quả dựa trên thông tin từ 'context'
        if (sw.ElapsedMilliseconds > 500) // Nếu chạy chậm hơn 500ms thì cảnh báo
        {
            _logger.LogWarning("API {Method} {Path} chạy chậm: {Elapsed}ms",
                context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation("API {Path} hoàn thành trong {Elapsed}ms",
                context.Request.Path, sw.ElapsedMilliseconds);
        }
    }
}