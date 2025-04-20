using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class User
    {
        public int UserId { get; set; } // PK

        // Foreign Key to Employee
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!; // Required relationship

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)] // Length depends on hashing algorithm output
        public string PasswordHash { get; set; } = string.Empty; // Store HASHED password

        // Foreign Key to Role
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!; // Required relationship

        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties for Auditing (Optional but good practice)
        public virtual ICollection<Shift> CreatedShifts { get; set; } = new List<Shift>();
        public virtual ICollection<Shift> UpdatedShifts { get; set; } = new List<Shift>();
        public virtual ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<LeaveRequest> CreatedLeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<LeaveRequest> UpdatedLeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
