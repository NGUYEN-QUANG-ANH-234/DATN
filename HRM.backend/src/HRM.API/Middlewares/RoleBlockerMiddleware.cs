using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using System.Text.Json;

namespace HRM.backend.src.HRM.API.Middlewares
{
    public class RoleBlockerMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleBlockerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Dùng DI tiêm IAppCache (hoặc UseCase) ngay trong InvokeAsync vì nó là Scoped Service
        public async Task InvokeAsync(HttpContext context)
        {
            // Lấy thông tin API mà Request đang trỏ tới
            var endpoint = context.GetEndpoint();

            // Lấy Attribute yêu cầu quyền (Ta sẽ định nghĩa RequirePermissionAttribute sau)
            var requiredPermission = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>()?.PermissionCode;

            if (!string.IsNullOrEmpty(requiredPermission))
            {
                var roleIdClaim = context.User.FindFirst("RoleId")?.Value;

                if (int.TryParse(roleIdClaim, out int roleId))
                {
                    // LỚP BẢO VỆ 3: ADMIN BẤT TỬ (Cho qua không cần check)
                    if (roleId != 1)
                    {
                        // Resolve UseCase từ RequestServices (tránh lỗi DI Scoped trong Middleware)
                        var rbacUseCase = context.RequestServices.GetRequiredService<IRbacUseCase>();

                        // Lấy ma trận quyền (Hàm này tự động lấy từ Cache rất nhanh)
                        var matrix = await rbacUseCase.GetAllRolesAndPermissionsAsync();

                        // Tìm quyền của Role hiện tại
                        var currentRole = matrix.FirstOrDefault(r => r.RoleId == roleId);

                        // Nếu không tìm thấy Role hoặc Role không có mã quyền yêu cầu -> CHẶN
                        if (currentRole == null || !currentRole.Permissions.Contains(requiredPermission))
                        {
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            {
                                success = false,
                                message = $"Truy cập bị từ chối. Bạn thiếu quyền: [{requiredPermission}]"
                            }));
                            return; // Dừng pipeline tại đây, không cho đi tiếp vào Controller
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await _next(context);
        }
    }

    // Class Attribute dùng để gắn lên đầu mỗi Controller/Action
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequirePermissionAttribute : Attribute
    {
        public string PermissionCode { get; }
        public string GroupName { get; set; } // Đổi thành public set
        public string Description { get; set; } // Đổi thành public set

        public RequirePermissionAttribute(string permissionCode)
        {
            PermissionCode = permissionCode;
            GroupName = "Chưa phân loại";
            Description = "Hệ thống tự động quét từ mã nguồn";
        }
    }
}
