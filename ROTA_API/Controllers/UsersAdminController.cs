using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;
using System.Security.Claims;

namespace ROTA_API.Controllers
{
    [Route("api/admin/users")] 
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class UsersAdminController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public UsersAdminController(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private int GetCurrentAdminUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) // Or JwtRegisteredClaimNames.Sub
                              ?? throw new InvalidOperationException("User ID claim not found in token.");
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Invalid User ID format in token.");
        }

        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetUsers()
        {
            var users = await _context.Users
                                    .Include(u => u.Role) // Need Role name
                                    .Include(u => u.Employee) // Need Employee name
                                    .OrderBy(u => u.Employee.LastName)
                                    .ThenBy(u => u.Employee.FirstName)
                                    .ToListAsync();

           
            var userDtos = _mapper.Map<IEnumerable<UserDetailDto>>(users);

            return Ok(userDtos);
        }

       
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDetailDto>> GetUser(int userId)
        {
            var user = await _context.Users
                                    .Include(u => u.Role)
                                    .Include(u => u.Employee)
                                    .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var userDto = _mapper.Map<UserDetailDto>(user);
            return Ok(userDto);
        }

       
        [HttpPatch("{userId}/role")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto updateUserRoleDto)
        {
            // 1. Find the user
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            // 2. Prevent admin from changing their own role (optional safety check)
            int currentAdminId = GetCurrentAdminUserId();
            if (userToUpdate.UserId == currentAdminId)
            {
                return BadRequest("Administrators cannot change their own role via this endpoint.");
            }

            // 3. Validate the new RoleId
            bool roleExists = await _context.Roles.AnyAsync(r => r.RoleId == updateUserRoleDto.RoleId);
            if (!roleExists)
            {
                return BadRequest($"Role with ID {updateUserRoleDto.RoleId} does not exist.");
            }

            // 4. Update the user's role
            userToUpdate.RoleId = updateUserRoleDto.RoleId;
            userToUpdate.UpdatedAt = DateTime.UtcNow; // Update timestamp

            // 5. Save changes
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user role {userId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the user role.");
            }

            return NoContent();
        }


     
        [HttpPatch("{userId}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDto updateUserStatusDto)
        {
            // 1. Find the user
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            // 2. Prevent admin from deactivating themselves (important safety check!)
            int currentAdminId = GetCurrentAdminUserId();
            if (userToUpdate.UserId == currentAdminId && !updateUserStatusDto.IsActive)
            {
                return BadRequest("Administrators cannot deactivate their own account.");
            }

            // 3. Update the status
            userToUpdate.IsActive = updateUserStatusDto.IsActive;
            userToUpdate.UpdatedAt = DateTime.UtcNow; // Update timestamp

            // 4. Save changes
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user status {userId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the user status.");
            }

            return NoContent();
        }


     
    }
}