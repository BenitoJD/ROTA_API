using System.ComponentModel.DataAnnotations;

namespace ROTA_API.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Confirmation password is required.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
