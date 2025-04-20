using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateShiftTypeDto
    {
        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = string.Empty;

        [Required] // Always provide this on update
        public bool IsOnCall { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
