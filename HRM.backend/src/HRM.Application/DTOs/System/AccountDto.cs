using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs
{
    public class GoogleLoginDto { 
        public required string Code { get; set; } 
    }

    public class CreateAccountDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }
    }

    public class ToggleStatusDto
    {
        [Required]
        public AccountStatus Status { get; set; } // Ví dụ: 0 = Active, 1 = Locked
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
