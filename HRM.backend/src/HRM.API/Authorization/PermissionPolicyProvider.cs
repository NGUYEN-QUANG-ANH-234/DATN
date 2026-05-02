using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HRM.backend.src.HRM.API.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Kiểm tra xem policy đã tồn tại (như "AdminOnly") chưa
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null) return policy;

        // Nếu chưa có (ví dụ: "EMPLOYEE_EDIT"), tự động tạo ra một Policy yêu cầu quyền đó
        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}