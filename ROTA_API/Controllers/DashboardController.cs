using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ROTA_API.DTOs;
using ROTA_API.Services;
using System.Security.Claims;

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: api/dashboard/oncall/upcoming?days=7&teamId=1
        /// <summary>
        /// Gets the upcoming on-call schedule for a specified period.
        /// </summary>
        /// <param name="days">Number of days from today to show (defaults to 7).</param>
        /// <param name="startDate">Specific start date (overrides days).</param>
        /// <param name="endDate">Specific end date (required if startDate is provided).</param>
        /// <param name="teamId">Optional team ID to filter by.</param>
        /// <returns>A list of dates with corresponding on-call assignments.</returns>
        [HttpGet("oncall/upcoming")]
        [ProducesResponseType(typeof(IEnumerable<UpcomingOnCallDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<UpcomingOnCallDto>>> GetUpcomingOnCall(
            [FromQuery] int days = 7, // Default to 7 days
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? teamId = null)
        {
            // Determine date range
            DateTime effectiveStartDate;
            DateTime effectiveEndDate;

            if (startDate.HasValue && endDate.HasValue)
            {
                if (endDate.Value < startDate.Value)
                {
                    return BadRequest("End date cannot be earlier than start date.");
                }
                effectiveStartDate = startDate.Value.Date;
                effectiveEndDate = endDate.Value.Date;
            }
            else if (startDate.HasValue) // Start date without end date is invalid for range
            {
                return BadRequest("End date must be provided if start date is specified.");
            }
            else // Default to 'days' parameter
            {
                if (days <= 0) days = 7; // Ensure positive days
                effectiveStartDate = DateTime.UtcNow.Date;
                effectiveEndDate = effectiveStartDate.AddDays(days - 1); // -1 because range includes start day
            }


            try
            {
                var result = await _dashboardService.GetUpcomingOnCallAsync(effectiveStartDate, effectiveEndDate, teamId);
                return Ok(result);
            }
            catch (Exception ex) // Catch unexpected errors from service
            {
                Console.WriteLine($"Error getting upcoming on-call: {ex}");
                // Use ProblemDetails from global handler or return specific code
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the on-call schedule.");
            }
        }
        // GET: api/dashboard/shifts/coverage?startDate=...&endDate=...&teamId=...&shiftTypeId=...&groupByTeam=true
        /// <summary>
        /// Gets a summary of shift coverage over a period, optionally grouped by team.
        /// </summary>
        /// <param name="startDate">The start date for the summary range.</param>
        /// <param name="endDate">The end date for the summary range.</param>
        /// <param name="teamId">Optional team ID to filter by.</param>
        /// <param name="shiftTypeId">Optional shift type ID to filter by.</param>
        /// <param name="groupByTeam">Set to true to get results per team, false for overall daily summary (defaults to false).</param>
        /// <returns>A list of daily coverage summaries.</returns>
        [HttpGet("shifts/coverage")]
        [ProducesResponseType(typeof(IEnumerable<ShiftCoverageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // Add Admin role requirement if needed
        public async Task<ActionResult<IEnumerable<ShiftCoverageDto>>> GetShiftCoverage(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? teamId = null,
            [FromQuery] int? shiftTypeId = null,
            [FromQuery] bool groupByTeam = false) // Default to overall summary
        {
            // Default date range if none provided (e.g., past 7 days)
            var effectiveEndDate = (endDate ?? DateTime.UtcNow).Date;
            var effectiveStartDate = (startDate ?? effectiveEndDate.AddDays(-6)).Date; // Default to 7 days including today

            if (effectiveEndDate < effectiveStartDate)
            {
                return BadRequest("End date cannot be earlier than start date.");
            }

            try
            {
                var result = await _dashboardService.GetShiftCoverageAsync(
                    effectiveStartDate, effectiveEndDate, teamId, shiftTypeId, groupByTeam);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting shift coverage: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching shift coverage.");
            }
        }
        [HttpGet("leave/summary")]
        [ProducesResponseType(typeof(IEnumerable<LeaveSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // Add Admin role if needed, or keep open for general analysis (filtered by user if applicable earlier)
        public async Task<ActionResult<IEnumerable<LeaveSummaryDto>>> GetLeaveSummary(
           [FromQuery] LeaveSummaryRequestParams parameters) // Use the parameter object
        {
            // Basic validation on parameters
            if (parameters.StartDate.HasValue && parameters.EndDate.HasValue && parameters.EndDate.Value < parameters.StartDate.Value)
            {
                return BadRequest("End date cannot be earlier than start date.");
            }

            try
            {
                var result = await _dashboardService.GetLeaveSummaryAsync(parameters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting leave summary: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the leave summary.");
            }
        }
        // GET: api/dashboard/leave/pendingcount?teamId=1
        /// <summary>
        /// Gets the count of pending leave requests, optionally filtered by team. (Admin Only)
        /// </summary>
        /// <param name="teamId">Optional team ID to filter the count by.</param>
        /// <returns>A list containing the pending count(s).</returns>
        [HttpGet("leave/pendingcount")]
        [Authorize(Roles = "Admin")] // Restrict this to Admins
        [ProducesResponseType(typeof(IEnumerable<PendingCountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<PendingCountDto>>> GetPendingLeaveCount(
            [FromQuery] int? teamId = null)
        {
            try
            {
                var result = await _dashboardService.GetPendingLeaveCountAsync(teamId);
                // Service returns a list, which might be empty or have one item
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting pending leave count: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the pending leave count.");
            }
        }
        [HttpGet("availability/team/{teamId}")] // Team ID in the route
        [ProducesResponseType(typeof(TeamAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // Add Admin role if needed
        public async Task<ActionResult<TeamAvailabilityDto>> GetTeamAvailability(
           int teamId,
           [FromQuery] DateTime? startDate = null,
           [FromQuery] DateTime? endDate = null)
        {
            // Default dates: If only startDate, use it as single day. If neither, use today.
            var effectiveStartDate = (startDate ?? DateTime.UtcNow).Date;
            var effectiveEndDate = (endDate ?? effectiveStartDate).Date; // Default end is same as start

            if (effectiveEndDate < effectiveStartDate)
            {
                return BadRequest("End date cannot be earlier than start date.");
            }

            // Basic check for valid Team ID before calling service (optional)
            // bool teamExists = await _context.Teams.AnyAsync(t => t.TeamId == teamId); // Requires injecting DbContext again, avoid if possible
            // if (!teamExists) return NotFound($"Team with ID {teamId} not found.");

            try
            {
                var result = await _dashboardService.GetTeamAvailabilityAsync(teamId, effectiveStartDate, effectiveEndDate);
                if (result == null)
                {
                    return NotFound($"Team with ID {teamId} not found.");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting team availability for team {teamId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching team availability.");
            }
        }
        [HttpGet("leave/trends")]
        [ProducesResponseType(typeof(IEnumerable<LeaveTrendPointDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // Add Admin role if sensitive or keep open for general analysis
        public async Task<ActionResult<IEnumerable<LeaveTrendPointDto>>> GetLeaveTrends(
           [FromQuery] LeaveTrendRequestParams parameters)
        {
            // Basic validation
            if (parameters.StartDate.HasValue && parameters.EndDate.HasValue && parameters.EndDate.Value < parameters.StartDate.Value)
            {
                return BadRequest("End date cannot be earlier than start date.");
            }

            try
            {
                var result = await _dashboardService.GetLeaveTrendsAsync(parameters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting leave trends: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching leave trends.");
            }
        }
        // Add to Controllers/DashboardController.cs
        // GET: api/dashboard/shifts/typedistribution?startDate=...&endDate=...&teamId=...
        /// <summary>
        /// Gets the distribution of different shift types worked over a period.
        /// </summary>
        [HttpGet("shifts/typedistribution")]
        [ProducesResponseType(typeof(IEnumerable<ShiftTypeDistributionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<ShiftTypeDistributionDto>>> GetShiftTypeDistribution(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? teamId = null)
        {
            var effectiveEndDate = (endDate ?? DateTime.UtcNow).Date;
            var effectiveStartDate = (startDate ?? effectiveEndDate.AddDays(-29)).Date; // Default 30 days

            if (effectiveEndDate < effectiveStartDate) return BadRequest("End date cannot be earlier than start date.");

            try
            {
                var result = await _dashboardService.GetShiftTypeDistributionAsync(effectiveStartDate, effectiveEndDate, teamId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting shift type distribution: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching shift type distribution.");
            }
        }
        // GET: api/dashboard/employee/5/schedule?startDate=...&endDate=...
        /// <summary>
        /// Gets the combined shift and leave schedule for a specific employee.
        /// </summary>
        /// <param name="employeeId">ID of the employee.</param>
        /// <param name="startDate">Start date for the schedule.</param>
        /// <param name="endDate">End date for the schedule.</param>
        [HttpGet("employee/{employeeId}/schedule")]
        [ProducesResponseType(typeof(IEnumerable<ScheduleItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // Allow any authenticated user to see their own? Or just Admin? Let's keep it open for now.
        public async Task<ActionResult<IEnumerable<ScheduleItemDto>>> GetEmployeeSchedule(
            int employeeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Authorization Check: Allow user to see their own schedule, or Admin see anyone's
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
                { return Unauthorized(); }

                // Need to get EmployeeId associated with currentUserId
                // This requires DbContext access OR another service call. Let's add a helper to DashboardService for this example
                // Ideally, have a dedicated IUserService for this.
                var currentUserEmployeeId = await GetCurrentUserEmployeeIdAsync_Helper(); // Implement this helper
                if (currentUserEmployeeId == null || currentUserEmployeeId.Value != employeeId)
                {
                    return Forbid("You can only view your own schedule.");
                }
            }
            // End Auth Check


            var effectiveEndDate = (endDate ?? DateTime.UtcNow.AddDays(14)).Date; // Default 2 weeks
            var effectiveStartDate = (startDate ?? DateTime.UtcNow).Date; // Default today

            if (effectiveEndDate < effectiveStartDate) return BadRequest("End date cannot be earlier than start date.");

            // Check if employee exists (optional pre-check)
            // bool employeeExists = await _context.Employees.AnyAsync(e => e.EmployeeId == employeeId && e.IsActive); // Requires context
            // if (!employeeExists) return NotFound($"Employee with ID {employeeId} not found.");


            try
            {
                var result = await _dashboardService.GetEmployeeScheduleAsync(employeeId, effectiveStartDate, effectiveEndDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting employee schedule for {employeeId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the employee schedule.");
            }
        }

        // Temporary Helper - Move to a proper User Service later
        private async Task<int?> GetCurrentUserEmployeeIdAsync_Helper()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                // Need DbContext here - this shows why logic should be in services that CAN access context
                // For demonstration, assume we add temporary context injection *or* this logic moves fully into service layer
                // using var scope = HttpContext.RequestServices.CreateScope(); // Get services if needed
                // var context = scope.ServiceProvider.GetRequiredService<RotaDbContext>();
                // return await context.Users...;
                // --- OR ---
                // Assume DashboardService exposes a helper (better)
                // return await _dashboardService.GetUserEmployeeId(userId);
                return null; // Placeholder - Implement properly
            }
            return null;
        }
        // GET: api/dashboard/oncall/gaps?requiredShiftTypeId=3&startDate=...&endDate=...
        /// <summary>
        /// Identifies periods where a required on-call shift type was not covered. (Admin Only)
        /// </summary>
        /// <param name="requiredShiftTypeId">The ShiftType ID that must be covered.</param>
        /// <param name="startDate">Start date for analysis.</param>
        /// <param name="endDate">End date for analysis.</param>
        [HttpGet("oncall/gaps")]
        [Authorize(Roles = "Admin")] // Likely an Admin function
        [ProducesResponseType(typeof(IEnumerable<OnCallGapDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<OnCallGapDto>>> GetOnCallGaps(
            [FromQuery] int requiredShiftTypeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var effectiveEndDate = (endDate ?? DateTime.UtcNow).Date;
            var effectiveStartDate = (startDate ?? effectiveEndDate.AddDays(-6)).Date; // Default 7 days

            if (effectiveEndDate < effectiveStartDate) return BadRequest("End date cannot be earlier than start date.");
            if (requiredShiftTypeId <= 0) return BadRequest("A valid requiredShiftTypeId must be provided.");

            try
            {
                // Optional: Pre-check if ShiftType exists and IsOnCall here? Or let service handle.
                var result = await _dashboardService.GetOnCallGapsAsync(requiredShiftTypeId, effectiveStartDate, effectiveEndDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting on-call gaps: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching on-call gaps.");
            }
        }
    }
}
