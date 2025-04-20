using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class ShiftType
    {
        public int ShiftTypeId { get; set; } // PK

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = string.Empty;

        // --- Added On-Call Flag ---
        public bool IsOnCall { get; set; } = false; // Default to false
        // --- End On-Call Flag ---

        public string? Description { get; set; }

        // Navigation Property
        public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }

}
