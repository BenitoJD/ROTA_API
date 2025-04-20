using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class Team
    {
        public int TeamId { get; set; } // PK

        [Required]
        [MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property: Employees belonging to this team
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
