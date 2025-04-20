using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class LeaveTypesController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public LeaveTypesController(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes()
        {
            var leaveTypes = await _context.LeaveTypes
                                           .OrderBy(lt => lt.LeaveTypeName)
                                           .ToListAsync();

            var leaveTypeDtos = _mapper.Map<IEnumerable<LeaveTypeDto>>(leaveTypes);
            return Ok(leaveTypeDtos);
        }

       
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeaveTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LeaveTypeDto>> GetLeaveType(int id)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);

            if (leaveType == null)
            {
                return NotFound($"Leave Type with ID {id} not found.");
            }

            var leaveTypeDto = _mapper.Map<LeaveTypeDto>(leaveType);
            return Ok(leaveTypeDto);
        }

        
        [HttpPost]
        [ProducesResponseType(typeof(LeaveTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LeaveTypeDto>> PostLeaveType([FromBody] CreateLeaveTypeDto createLeaveTypeDto)
        {
            // Check if leave type name already exists (case-insensitive)
            bool nameExists = await _context.LeaveTypes.AnyAsync(lt => lt.LeaveTypeName.ToLower() == createLeaveTypeDto.LeaveTypeName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(CreateLeaveTypeDto.LeaveTypeName), $"A leave type with the name '{createLeaveTypeDto.LeaveTypeName}' already exists.");
                return BadRequest(ModelState);
            }

            var leaveType = _mapper.Map<LeaveType>(createLeaveTypeDto);

            // Note: CreatedAt/UpdatedAt aren't in the LeaveType model, so no need to set them.

            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();

            var leaveTypeDto = _mapper.Map<LeaveTypeDto>(leaveType);

            return CreatedAtAction(nameof(GetLeaveType), new { id = leaveType.LeaveTypeId }, leaveTypeDto);
        }

        
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutLeaveType(int id, [FromBody] UpdateLeaveTypeDto updateLeaveTypeDto)
        {
            var leaveTypeToUpdate = await _context.LeaveTypes.FindAsync(id);

            if (leaveTypeToUpdate == null)
            {
                return NotFound($"Leave Type with ID {id} not found.");
            }

            // Check if the NEW name already exists for a DIFFERENT leave type (case-insensitive)
            bool nameExists = await _context.LeaveTypes.AnyAsync(lt =>
                lt.LeaveTypeId != id &&
                lt.LeaveTypeName.ToLower() == updateLeaveTypeDto.LeaveTypeName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(UpdateLeaveTypeDto.LeaveTypeName), $"Another leave type with the name '{updateLeaveTypeDto.LeaveTypeName}' already exists.");
                return BadRequest(ModelState);
            }

            // Map updated values
            _mapper.Map(updateLeaveTypeDto, leaveTypeToUpdate);

            // No timestamp update needed here

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.LeaveTypes.AnyAsync(lt => lt.LeaveTypeId == id)) { return NotFound(); } else { throw; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating leave type {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the leave type.");
            }


            return NoContent();
        }

        
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // If referenced by leave requests
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteLeaveType(int id)
        {
            var leaveTypeToDelete = await _context.LeaveTypes.FindAsync(id);

            if (leaveTypeToDelete == null)
            {
                return NotFound($"Leave Type with ID {id} not found.");
            }

            // Check if any LeaveRequests use this type BEFORE attempting delete
            bool isInUse = await _context.LeaveRequests.AnyAsync(lr => lr.LeaveTypeId == id);
            if (isInUse)
            {
                // Provide a helpful error message instead of letting the DB constraint fail opaquely
                return BadRequest($"Cannot delete Leave Type '{leaveTypeToDelete.LeaveTypeName}': It is currently referenced by existing leave requests. Reassign or delete those requests first.");
            }

            _context.LeaveTypes.Remove(leaveTypeToDelete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // Catch potential DB errors (though the check above should prevent FK issues)
            {
                Console.WriteLine($"Error deleting leave type {id}: {ex}");
                // You might get here if there's a race condition or unexpected constraint
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the leave type. It might still be in use.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting leave type {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the leave type.");
            }

            return NoContent();
        }
    }
}
