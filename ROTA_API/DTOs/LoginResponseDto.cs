namespace ROTA_API.DTOs
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; } // Null if login fails
        public DateTime? Expiration { get; set; } //  When the token expires
    }
    
}
