using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Lấy UserID từ Claim của JWT
            // Thay vì tìm "id", hãy tìm NameIdentifier
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                                
                if (!int.TryParse(userIdClaim, out var userId)) return;

                // Logic: Admin có toàn bộ quyền; các role khác phải có permission cụ thể.
                var hasPermission = await dbContext.Accounts
                    .AnyAsync(a => a.Id == userId &&
                        a.Role != null &&
                        (a.Role.RoleName == "Admin" ||
                         a.Role.RolePermissions.Any(rp => rp.Permission.PermissionCode == requirement.Permission)));

                if (hasPermission)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
