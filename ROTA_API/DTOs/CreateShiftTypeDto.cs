using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class CreateShiftTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = string.Empty;

        public bool IsOnCall { get; set; } = false; // Default but allow override

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
