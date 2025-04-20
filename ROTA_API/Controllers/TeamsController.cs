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
    public class TeamsController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public TeamsController(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TeamDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<TeamDto>>> GetTeams()
        {
            var teams = await _context.Teams
                                    .Include(t => t.Employees)
                                    .OrderBy(t => t.TeamName)
                                    .ToListAsync();

            // Map to DTOs
            var teamDtos = _mapper.Map<IEnumerable<TeamDto>>(teams);
            return Ok(teamDtos);
        }

        // GET: api/teams/5
        /// <summary>
        /// Retrieves a specific team by ID. (Admin Only)
        /// </summary>
        /// <param name="id">The ID of the team to retrieve.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TeamDto>> GetTeam(int id)
        {
            // Optional: Include employees if detail view needs them
            //var team = await _context.Teams.Include(t => t.Employees).FirstOrDefaultAsync(t => t.TeamId == id);
            var team = await _context.Teams.FindAsync(id);

            if (team == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            var teamDto = _mapper.Map<TeamDto>(team);
            return Ok(teamDto);
        }

        // POST: api/teams
        /// <summary>
        /// Creates a new team. (Admin Only)
        /// </summary>
        /// <param name="createTeamDto">Data for the new team.</param>
        [HttpPost]
        [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TeamDto>> PostTeam([FromBody] CreateTeamDto createTeamDto)
        {
            // Check if team name already exists (case-insensitive)
            bool nameExists = await _context.Teams.AnyAsync(t => t.TeamName.ToLower() == createTeamDto.TeamName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(CreateTeamDto.TeamName), $"A team with the name '{createTeamDto.TeamName}' already exists.");
                return BadRequest(ModelState);
            }

            var team = _mapper.Map<Team>(createTeamDto);

            // Set audit timestamps
            team.CreatedAt = DateTime.UtcNow;
            team.UpdatedAt = DateTime.UtcNow;

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Map the created entity back to DTO for response
            var teamDto = _mapper.Map<TeamDto>(team);

            return CreatedAtAction(nameof(GetTeam), new { id = team.TeamId }, teamDto);
        }

        // PUT: api/teams/5
        /// <summary>
        /// Updates an existing team. (Admin Only)
        /// </summary>
        /// <param name="id">The ID of the team to update.</param>
        /// <param name="updateTeamDto">The updated team data.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutTeam(int id, [FromBody] UpdateTeamDto updateTeamDto)
        {
            var teamToUpdate = await _context.Teams.FindAsync(id);

            if (teamToUpdate == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            // Check if the NEW name already exists for a DIFFERENT team (case-insensitive)
            bool nameExists = await _context.Teams.AnyAsync(t =>
                t.TeamId != id && // Exclude the current team
                t.TeamName.ToLower() == updateTeamDto.TeamName.ToLower());
            if (nameExists)
            {
                ModelState.AddModelError(nameof(UpdateTeamDto.TeamName), $"Another team with the name '{updateTeamDto.TeamName}' already exists.");
                return BadRequest(ModelState);
            }

            // Map updated values onto the existing tracked entity
            _mapper.Map(updateTeamDto, teamToUpdate);

            // Update timestamp
            teamToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Teams.AnyAsync(e => e.TeamId == id)) { return NotFound(); } else { throw; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating team {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the team.");
            }

            return NoContent();
        }

        // DELETE: api/teams/5
        /// <summary>
        /// Deletes a team. Employees previously in this team will have their TeamId set to null. (Admin Only)
        /// </summary>
        /// <param name="id">The ID of the team to delete.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var teamToDelete = await _context.Teams.FindAsync(id);

            if (teamToDelete == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            bool hasEmployees = await _context.Employees.AnyAsync(e => e.TeamId == id && e.IsActive);
            if (hasEmployees)
            {
                return BadRequest("Cannot delete team: Active employees are still assigned to it. Reassign employees first.");
            }

            _context.Teams.Remove(teamToDelete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) // Catch potential errors
            {
                Console.WriteLine($"Error deleting team {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the team.");
            }

            return NoContent();
        }
    }
}
