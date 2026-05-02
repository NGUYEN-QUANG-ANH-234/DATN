namespace HRM.backend.src.HRM.API.Authorization
{
    using Microsoft.AspNetCore.Authorization;
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission) => Permission = permission;
    }
}
