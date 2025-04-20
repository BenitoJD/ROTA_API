using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt; 

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper; 

        public AuthController(RotaDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper; 
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous] 
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            var user = await _context.Users
                                     .Include(u => u.Role) 
                                     .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || !user.IsActive || user.Role == null) 
            {
                return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid username or password." });
            }

            bool isPasswordValid = BCryptNet.Verify(loginDto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid username or password." });
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Key not configured"));

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // Subject = User ID
                    new Claim(JwtRegisteredClaimNames.Name, user.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                    new Claim(ClaimTypes.Role, user.Role.RoleName), 
                    new Claim("employeeId", user.EmployeeId.ToString())
                };
                var duration = _configuration.GetValue<double>("Jwt:DurationInMinutes", 60);
                var expirationTime = DateTime.UtcNow.AddMinutes(duration);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expirationTime,
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();


                return Ok(new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = tokenString,
                    Expiration = tokenDescriptor.Expires // Send expiration back
                });
            }
            catch (Exception ex) 
            {
                // Log the exception ex
                Console.WriteLine($"Error generating token: {ex}"); // Basic logging
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResponseDto { Success = false, Message = "An error occurred during login." });
            }
        }


        // POST: api/auth/register
        [HttpPost("register")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest("Username already exists.");
            }

            if (!await _context.Employees.AnyAsync(e => e.EmployeeId == registerDto.EmployeeId && e.IsActive))
            {
                return BadRequest("Invalid or inactive Employee ID.");
            }

            if (!await _context.Roles.AnyAsync(r => r.RoleId == registerDto.RoleId))
            {
                return BadRequest("Invalid Role ID.");
            }

            if (await _context.Users.AnyAsync(u => u.EmployeeId == registerDto.EmployeeId))
            {
                return BadRequest("This Employee already has a User account.");
            }

            string hashedPassword = BCryptNet.HashPassword(registerDto.Password);

            var newUser = new User
            {
                EmployeeId = registerDto.EmployeeId,
                Username = registerDto.Username,
                PasswordHash = hashedPassword,
                RoleId = registerDto.RoleId,
                IsActive = true, 
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully." });
        }

        // GET: api/auth/me
        /// <summary>
        /// Gets the profile details for the currently authenticated user.
        /// </summary>
        [HttpGet("me")]
        [Authorize] // User must be logged in
        [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDetailDto>> GetCurrentUserProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); // Use standard NameIdentifier claim

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid token or User ID claim missing.");
            }

            var user = await _context.Users
                                    .Include(u => u.Role)
                                    .Include(u => u.Employee) // Eager load Employee
                                    .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("User profile not found.");
            if (user.Employee == null) return NotFound("User profile is incomplete (missing employee link)."); // Check link
            if (user.Role == null) return NotFound("User profile is incomplete (missing role link)."); // Check link

            // Use AutoMapper to map
            var userDto = _mapper.Map<UserDetailDto>(user);

            return Ok(userDto);
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            // Implementation needed here or call IUserService.ChangeUserPasswordAsync
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid user identifier in token.");
            }

            // --- Example implementation directly in controller ---
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive) return NotFound("User not found or inactive.");
            if (!BCryptNet.Verify(changePasswordDto.CurrentPassword, user.PasswordHash)) return BadRequest("Incorrect current password."); // Use BadRequest (400) for incorrect current password
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword) return BadRequest("New passwords do not match.");
            if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword) || changePasswordDto.NewPassword.Length < 8) return BadRequest("New password does not meet length requirements."); // Basic validation

            user.PasswordHash = BCryptNet.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent(); // Success
        }
    }
}
