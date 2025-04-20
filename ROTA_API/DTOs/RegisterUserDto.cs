using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class RegisterUserDto
    {
        [Required]
        public int EmployeeId { get; set; } // Link to an existing employee

        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(8)] // Enforce minimum password length
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; } // Assign Role (e.g., 1 for Admin, 2 for Viewer)
    }
}
