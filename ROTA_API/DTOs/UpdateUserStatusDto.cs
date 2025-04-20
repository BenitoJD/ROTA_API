using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateUserStatusDto
    {
        [Required]
        public bool IsActive { get; set; } // The new active status
    }
}
