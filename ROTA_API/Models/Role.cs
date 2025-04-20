using System.ComponentModel.DataAnnotations;

namespace ROTA_API.Models
{
    public class Role
    {
        public int RoleId { get; set; } // PK

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        // Permissions
        public bool CanEditRota { get; set; } = false;
        public bool CanEditLeave { get; set; } = false;
        public bool CanApproveLeave { get; set; } = false;
        public bool CanViewRota { get; set; } = true;
        public bool CanViewLeave { get; set; } = true;

        public string? Description { get; set; }

        // Navigation Property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
