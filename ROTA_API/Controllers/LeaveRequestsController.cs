using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;
using ROTA_API.Services;
using System.Security.Claims;

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveRequestService;

        public LeaveRequestsController(ILeaveRequestService leaveRequestService)
        {
            _leaveRequestService = leaveRequestService;
        }


        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); // Or JwtRegisteredClaimNames.Sub
            if (userIdClaim != null && int.TryParse(userIdClaim, out userId))
            {
                return true;
            }
            Console.WriteLine("Warning: Could not parse User ID claim."); // Log this issue
            return false;
        }

        private bool IsCurrentUserAdmin()
        {
            return User.IsInRole("Admin"); // Check if user has Admin role claim
        }


        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequests(
            [FromQuery] int? employeeId, [FromQuery] LeaveStatus? status,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
            [FromQuery] int? leaveTypeId, [FromQuery] int? teamId)
        {
            if (!TryGetCurrentUserId(out int currentUserId)) return Unauthorized("Invalid user token.");
            bool isAdmin = IsCurrentUserAdmin();

            var leaveRequestDtos = await _leaveRequestService.GetLeaveRequestsAsync(
                currentUserId, isAdmin, employeeId, status, startDate, endDate, leaveTypeId, teamId);

            return Ok(leaveRequestDtos);
        }

        // GET: api/leaverequests/10
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Service returns null if forbidden
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(int id)
        {
            if (!TryGetCurrentUserId(out int currentUserId)) return Unauthorized("Invalid user token.");
            bool isAdmin = IsCurrentUserAdmin();

            var leaveRequestDto = await _leaveRequestService.GetLeaveRequestByIdAsync(id, currentUserId, isAdmin);

            if (leaveRequestDto == null)
            {
                // Could be Not Found OR Forbidden, return NotFound for simplicity,
                // or add more complex logic to return 403 if needed.
                return NotFound($"Leave Request with ID {id} not found or access denied.");
            }

            return Ok(leaveRequestDto);
        }

        // POST: api/leaverequests
        [HttpPost]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LeaveRequestDto>> PostLeaveRequest([FromBody] CreateLeaveRequestDto createLeaveRequestDto)
        {
            if (!TryGetCurrentUserId(out int currentUserId)) return Unauthorized("Invalid user token.");
            bool isAdmin = IsCurrentUserAdmin();

            try
            {
                var createdDto = await _leaveRequestService.CreateLeaveRequestAsync(createLeaveRequestDto, currentUserId, isAdmin);
                return CreatedAtAction(nameof(GetLeaveRequest), new { id = createdDto.LeaveRequestId }, createdDto);
            }
            catch (ArgumentException ex) // Validation errors
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (InvalidOperationException ex) // Overlap errors etc.
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (UnauthorizedAccessException ex) // Permission errors
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex) // Unexpected errors
            {
                Console.WriteLine($"Unexpected error creating leave request: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // PATCH: api/leaverequests/10/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")] // Only Admins use this controller action
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LeaveRequestDto>> UpdateLeaveRequestStatus(int id, [FromBody] UpdateLeaveStatusDto updateStatusDto)
        {
            if (!TryGetCurrentUserId(out int adminUserId)) return Unauthorized("Invalid user token."); // Should have Admin role

            try
            {
                var updatedDto = await _leaveRequestService.UpdateLeaveRequestStatusAsync(id, updateStatusDto, adminUserId);
                if (updatedDto == null)
                {
                    return NotFound($"Leave Request with ID {id} not found.");
                }
                return Ok(updatedDto);
            }
            catch (ArgumentException ex) // Invalid target status
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (InvalidOperationException ex) // Not pending, overlap on approval etc.
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex) // Unexpected errors
            {
                Console.WriteLine($"Unexpected error updating leave request status {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // PATCH: api/leaverequests/10/cancel
        [HttpPatch("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // e.g., trying to cancel a rejected request
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelLeaveRequest(int id)
        {
            if (!TryGetCurrentUserId(out int currentUserId)) return Unauthorized("Invalid user token.");
            bool isAdmin = IsCurrentUserAdmin();

            try
            {
                bool success = await _leaveRequestService.CancelLeaveRequestAsync(id, currentUserId, isAdmin);
                if (!success)
                {
                    bool exists = await _leaveRequestService.DoesLeaveRequestExistAsync(id); 
                    if (!exists)
                    {
                        return NotFound($"Leave Request with ID {id} not found.");
                    }
                    else
                    {
                        return BadRequest($"Leave Request with ID {id} cannot be cancelled in its current state.");
                    }
                }
                return NoContent(); 
            }
            catch (UnauthorizedAccessException ex) 
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Unexpected error cancelling leave request {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }
    }
}

