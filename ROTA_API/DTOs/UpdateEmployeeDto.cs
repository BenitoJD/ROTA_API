using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class UpdateEmployeeDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        public int? TeamId { get; set; } 

        public bool IsActive { get; set; } = true; 
    }
}
