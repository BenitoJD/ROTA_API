using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateUserRoleDto
    {
        [Required]
        public int RoleId { get; set; } // The new Role ID to assign
    }
}
