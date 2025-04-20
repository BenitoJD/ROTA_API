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
    public class ShiftTypesController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public ShiftTypesController(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/shifttypes
        /// <summary>
        /// Retrieves a list of all shift types. (Admin Only)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ShiftTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ShiftTypeDto>>> GetShiftTypes()
        {
            var shiftTypes = await _context.ShiftTypes
                                           .OrderBy(st => st.TypeName)
                                           .ToListAsync();

            var shiftTypeDtos = _mapper.Map<IEnumerable<ShiftTypeDto>>(shiftTypes);
            return Ok(shiftTypeDtos);
        }

        // GET: api/shifttypes/5
        /// <summary>
        /// Retrieves a specific shift type by ID. (Admin Only)
        /// </summary>
        /// <param name="id">The ID of the shift type to retrieve.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ShiftTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ShiftTypeDto>> GetShiftType(int id)
        {
            var shiftType = await _context.ShiftTypes.FindAsync(id);

            if (shiftType == null)
            {
                return NotFound($"Shift Type with ID {id} not found.");
            }

            var shiftTypeDto = _mapper.Map<ShiftTypeDto>(shiftType);
            return Ok(shiftTypeDto);
        }

        // POST: api/shifttypes
        /// <summary>
        /// Creates a new shift type. (Admin Only)
        /// </summary>
        /// <param name="createShiftTypeDto">Data for the new shift type.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ShiftTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ShiftTypeDto>> PostShiftType([FromBody] CreateShiftTypeDto createShiftTypeDto)
        {
            // Check if shift type name already exists (case-insensitive)
            bool nameExists = await _context.ShiftTypes.AnyAsync(st => st.TypeName.ToLower() == createShiftTypeDto.TypeName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(CreateShiftTypeDto.TypeName), $"A shift type with the name '{createShiftTypeDto.TypeName}' already exists.");
                return BadRequest(ModelState);
            }

            var shiftType = _mapper.Map<ShiftType>(createShiftTypeDto);

            // Note: CreatedAt/UpdatedAt aren't in the ShiftType model

            _context.ShiftTypes.Add(shiftType);
            await _context.SaveChangesAsync();

            var shiftTypeDto = _mapper.Map<ShiftTypeDto>(shiftType);

            return CreatedAtAction(nameof(GetShiftType), new { id = shiftType.ShiftTypeId }, shiftTypeDto);
        }

        // PUT: api/shifttypes/5
        /// <summary>
        /// Updates an existing shift type. (Admin Only)
        /// </summary>
        /// <param name="id">The ID of the shift type to update.</param>
        /// <param name="updateShiftTypeDto">The updated shift type data.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutShiftType(int id, [FromBody] UpdateShiftTypeDto updateShiftTypeDto)
        {
            var shiftTypeToUpdate = await _context.ShiftTypes.FindAsync(id);

            if (shiftTypeToUpdate == null)
            {
                return NotFound($"Shift Type with ID {id} not found.");
            }

            // Check if the NEW name already exists for a DIFFERENT shift type (case-insensitive)
            bool nameExists = await _context.ShiftTypes.AnyAsync(st =>
                st.ShiftTypeId != id &&
                st.TypeName.ToLower() == updateShiftTypeDto.TypeName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(UpdateShiftTypeDto.TypeName), $"Another shift type with the name '{updateShiftTypeDto.TypeName}' already exists.");
                return BadRequest(ModelState);
            }

            // Map updated values
            _mapper.Map(updateShiftTypeDto, shiftTypeToUpdate);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ShiftTypes.AnyAsync(st => st.ShiftTypeId == id)) { return NotFound(); } else { throw; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating shift type {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the shift type.");
            }

            return NoContent();
        }

        // DELETE: api/shifttypes/5
        /// <summary>
        /// Deletes a shift type. (Admin Only)
        /// Note: Shifts using this type will have their ShiftTypeId set to NULL (due to ON DELETE SET NULL).
        /// </summary>
        /// <param name="id">The ID of the shift type to delete.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteShiftType(int id)
        {
            var shiftTypeToDelete = await _context.ShiftTypes.FindAsync(id);

            if (shiftTypeToDelete == null)
            {
                return NotFound($"Shift Type with ID {id} not found.");
            }

            // Unlike LeaveTypes (which had RESTRICT), deleting a ShiftType is allowed
            // even if Shifts reference it, because we configured ON DELETE SET NULL.
            // No need for an explicit check here, but be aware of the consequence.

            _context.ShiftTypes.Remove(shiftTypeToDelete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) // Catch potential errors
            {
                Console.WriteLine($"Error deleting shift type {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the shift type.");
            }

            return NoContent();
        }
    }
}
