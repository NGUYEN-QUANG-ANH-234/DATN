using System.Net;
using System.Text.Json;

namespace HRM.backend.src.HRM.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception at request {Path}. TraceId: {TraceId}",
                    context.Request.Path,
                    context.TraceIdentifier);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                    ArgumentException => (int)HttpStatusCode.BadRequest,
                    InvalidOperationException => (int)HttpStatusCode.UnprocessableEntity,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError
                        ? "Có lỗi xảy ra từ phía Server!"
                        : ex.Message,
                    TraceId = context.TraceIdentifier,
                    Detailed = _environment.IsDevelopment() ? ex.ToString() : null
                };

                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, jsonOptions);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
