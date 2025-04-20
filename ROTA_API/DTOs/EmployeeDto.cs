namespace ROTA_API.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; } 
        public bool IsActive { get; set; }
    }
}
