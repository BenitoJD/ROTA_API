using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class Shift
    {
        public int ShiftId { get; set; } // PK

        // Foreign Key to Employee
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        // Foreign Key to ShiftType (optional)
        public int? ShiftTypeId { get; set; } // Nullable if optional or if ShiftType table doesn't exist
        [ForeignKey("ShiftTypeId")]
        public virtual ShiftType? ShiftType { get; set; } // Nullable navigation property

        [Required]
        public DateTime ShiftStartDateTime { get; set; }
        [Required]
        public DateTime ShiftEndDateTime { get; set; }

        public string? Notes { get; set; }

        // Auditing Foreign Keys
        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public int? UpdatedByUserId { get; set; }
        [ForeignKey("UpdatedByUserId")]
        public virtual User? UpdatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
