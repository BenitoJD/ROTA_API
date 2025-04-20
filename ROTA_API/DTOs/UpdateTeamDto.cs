using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateTeamDto
    {
        [Required]
        [MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
