using AutoMapper;
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
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftsController(IShiftService shiftService) // Inject service
        {
            _shiftService = shiftService;
        }

        // GET: api/shifts?startDate=...&endDate=...&employeeId=...&teamId=...&isOnCall=...
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ShiftDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<ShiftDto>>> GetShifts(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? employeeId,
            [FromQuery] int? teamId,
            [FromQuery] bool? isOnCall)
        {
            var shiftDtos = await _shiftService.GetShiftsAsync(startDate, endDate, employeeId, teamId, isOnCall);
            return Ok(shiftDtos);
        }

        // GET: api/shifts/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShiftDto>> GetShift(int id)
        {
            var shiftDto = await _shiftService.GetShiftByIdAsync(id);
            if (shiftDto == null)
            {
                return NotFound($"Shift with ID {id} not found.");
            }
            return Ok(shiftDto);
        }

        // POST: api/shifts
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ShiftDto>> PostShift([FromBody] CreateShiftDto createShiftDto)
        {
            try
            {
                int creatorUserId = GetCurrentUserId(); // Get user ID here
                var createdShiftDto = await _shiftService.CreateShiftAsync(createShiftDto, creatorUserId);

                
                return CreatedAtAction(nameof(GetShift), new { id = createdShiftDto.ShiftId }, createdShiftDto);
                
            }
            catch (ArgumentException ex) // Catch validation errors from service
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (InvalidOperationException ex) // Catch overlap errors etc. from service
            {
                ModelState.AddModelError(string.Empty, ex.Message); // General model error
                return BadRequest(ModelState);
            }
            catch (Exception ex) // Catch unexpected errors
            {
                Console.WriteLine($"Unexpected error creating shift: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the shift.");
            }
        }

        // PUT: api/shifts/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutShift(int id, [FromBody] UpdateShiftDto updateShiftDto)
        {
            try
            {
                int updaterUserId = GetCurrentUserId();
                bool success = await _shiftService.UpdateShiftAsync(id, updateShiftDto, updaterUserId);

                if (!success)
                {
                    return NotFound($"Shift with ID {id} not found for update.");
                }
                return NoContent();
            }
            catch (ArgumentException ex) // Catch validation errors from service
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (InvalidOperationException ex) // Catch overlap errors etc. from service
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict, "The shift record was modified by another user.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error updating shift {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the shift.");
            }
        }

        // DELETE: api/shifts/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                bool success = await _shiftService.DeleteShiftAsync(id);
                if (!success)
                {
                    return NotFound($"Shift with ID {id} not found for deletion.");
                }
                return NoContent();
            }
            catch (Exception ex) // Catch unexpected errors re-thrown by service
            {
                Console.WriteLine($"Unexpected error deleting shift {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the shift.");
            }
        }


        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) // Or JwtRegisteredClaimNames.Sub
                              ?? throw new InvalidOperationException("User ID claim not found in token.");
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Invalid User ID format in token.");
        }
    }
}
