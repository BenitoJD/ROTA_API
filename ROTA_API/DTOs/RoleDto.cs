namespace ROTA_API.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        // Include permissions if the client needs them, otherwise omit
        public bool CanEditRota { get; set; }
        public bool CanEditLeave { get; set; }
        public bool CanApproveLeave { get; set; }
        public bool CanViewRota { get; set; }
        public bool CanViewLeave { get; set; }
        public string? Description { get; set; }
    }
}
