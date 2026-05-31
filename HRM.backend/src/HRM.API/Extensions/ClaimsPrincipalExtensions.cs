using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetAccountIdOrThrow(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var accountId))
                throw new UnauthorizedAccessException("Token không chứa mã tài khoản hợp lệ.");

            return accountId;
        }

        public static string GetRoleOrEmpty(this ClaimsPrincipal user)
        {
            return user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
