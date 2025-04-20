using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper; // Inject if using ProjectTo or manual mapping

        public DashboardService(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UpcomingOnCallDto>> GetUpcomingOnCallAsync(
            DateTime startDate, DateTime endDate, int? teamId)
        {
            // Ensure dates are treated as Date only for comparison start if needed,
            // but keep time for filtering overlaps accurately.
            var queryStartDate = startDate.Date; // Start of the day
            var queryEndDate = endDate.Date.AddDays(1); // End of the *last* day (exclusive)

            var onCallShiftsQuery = _context.Shifts
                .Include(s => s.Employee).ThenInclude(e => e.Team) // Include necessary navigation properties
                .Include(s => s.ShiftType)
                .Where(s => s.ShiftType != null && s.ShiftType.IsOnCall) // Filter for On-Call shifts
                                                                         // Filter for shifts that *overlap* with the requested date range
                .Where(s => s.ShiftStartDateTime < queryEndDate && s.ShiftEndDateTime > queryStartDate);

            // Apply optional Team filter
            if (teamId.HasValue)
            {
                onCallShiftsQuery = onCallShiftsQuery.Where(s => s.Employee.TeamId == teamId.Value);
            }

            // Fetch the relevant shifts
            var onCallShifts = await onCallShiftsQuery
                                     .OrderBy(s => s.ShiftStartDateTime) // Order chronologically
                                     .ThenBy(s => s.ShiftTypeId) // Optional: Consistent ordering for same start time
                                     .ToListAsync();

            // --- Process and Group the results by Date ---
            // This grouping logic is often done here in the service.
            var result = new List<UpcomingOnCallDto>();
            var dateRange = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                                     .Select(offset => startDate.Date.AddDays(offset));

            foreach (var date in dateRange)
            {
                var dayStart = date;
                var dayEnd = date.AddDays(1);

                var assignmentsForDate = onCallShifts
                    // Find shifts that are active *at any point* during this specific day
                    .Where(s => s.ShiftStartDateTime < dayEnd && s.ShiftEndDateTime > dayStart)
                    // Map to the DTO for this specific assignment
                    .Select(s => new OnCallAssignmentDto
                    {
                        EmployeeId = s.EmployeeId,
                        EmployeeName = $"{s.Employee.FirstName} {s.Employee.LastName}",
                        TeamId = s.Employee.TeamId,
                        TeamName = s.Employee.Team?.TeamName, // Use ?. for safety
                        ShiftTypeId = s.ShiftTypeId ?? 0, // Handle potential null ShiftTypeId if needed
                        ShiftTypeName = s.ShiftType?.TypeName ?? "Unknown", // Use ?. for safety
                        ShiftStartDateTime = s.ShiftStartDateTime,
                        ShiftEndDateTime = s.ShiftEndDateTime
                    })
                    .ToList();

                // Only add the date to the result if there are assignments for it
                if (assignmentsForDate.Any())
                {
                    result.Add(new UpcomingOnCallDto
                    {
                        Date = date,
                        Assignments = assignmentsForDate
                    });
                }
            }

            return result;

            // Alternative using ProjectTo (if DTO structure allows direct mapping)
            // This might be harder if you need the specific grouping logic by day *after* fetching.
            // If you just return List<OnCallAssignmentDto>, ProjectTo is great:
            // return await onCallShiftsQuery
            //            .OrderBy(...)
            //            .ProjectTo<OnCallAssignmentDto>(_mapper.ConfigurationProvider) // Requires mapping profile setup
            //            .ToListAsync();
        }
        public async Task<IEnumerable<ShiftCoverageDto>> GetShiftCoverageAsync(
          DateTime startDate, DateTime endDate, int? teamId, int? shiftTypeId, bool groupByTeam)
        {
            var queryStartDate = startDate.Date;
            var queryEndDate = endDate.Date.AddDays(1); // Exclusive end date

            // Base query for shifts overlapping the date range
            var shiftsQuery = _context.Shifts
                .Include(s => s.Employee).ThenInclude(e => e.Team) // Need Team info if grouping
                .Include(s => s.ShiftType) // Needed if filtering by ShiftType
                .Where(s => s.ShiftStartDateTime < queryEndDate && s.ShiftEndDateTime > queryStartDate);

            // Apply optional filters
            if (teamId.HasValue)
            {
                shiftsQuery = shiftsQuery.Where(s => s.Employee.TeamId == teamId.Value);
            }
            if (shiftTypeId.HasValue)
            {
                shiftsQuery = shiftsQuery.Where(s => s.ShiftTypeId == shiftTypeId.Value);
            }

            // Fetch all relevant shifts within the broad date range and filters first
            var relevantShifts = await shiftsQuery.ToListAsync();

            // --- Manual Processing & Grouping ---
            // This is often easier than complex LINQ for daily summaries when shifts span days.
            var coverageData = new List<ShiftCoverageDto>();
            var dateRange = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                                     .Select(offset => startDate.Date.AddDays(offset));

            foreach (var date in dateRange)
            {
                var dayStart = date;
                var dayEnd = date.AddDays(1);

                // Filter the fetched shifts for *this specific day*
                var shiftsForDay = relevantShifts
                    .Where(s => s.ShiftStartDateTime < dayEnd && s.ShiftEndDateTime > dayStart)
                    .ToList(); // Materialize shifts for this day

                if (!shiftsForDay.Any()) continue; // Skip days with no relevant shifts

                if (groupByTeam)
                {
                    // Group shifts for this day by Team
                    var groupedByTeam = shiftsForDay
                        .GroupBy(s => s.Employee.Team) // Group by the actual Team entity
                        .Select(g => new ShiftCoverageDto
                        {
                            Date = date,
                            TeamId = g.Key?.TeamId, // Team entity might be null if employee has no team
                            TeamName = g.Key?.TeamName ?? "Unassigned", // Handle null team
                            ShiftCount = g.Count(), // Count shifts in this group
                            UniqueEmployeeCount = g.Select(s => s.EmployeeId).Distinct().Count() // Count distinct employees
                        })
                        .ToList();
                    coverageData.AddRange(groupedByTeam);

                    // Optional: Add entry for teams *without* shifts on this day if needed
                    // Requires fetching all relevant teams first and doing a left-join style merge
                }
                else // Don't group by team, calculate overall coverage for the day
                {
                    coverageData.Add(new ShiftCoverageDto
                    {
                        Date = date,
                        TeamId = null, // Represents all teams
                        TeamName = "All Teams",
                        ShiftCount = shiftsForDay.Count,
                        UniqueEmployeeCount = shiftsForDay.Select(s => s.EmployeeId).Distinct().Count()
                    });
                }
            }

            return coverageData.OrderBy(c => c.Date).ThenBy(c => c.TeamName); // Order results
        }
        public async Task<IEnumerable<LeaveSummaryDto>> GetLeaveSummaryAsync(LeaveSummaryRequestParams parameters)
        {
            // Default date range (e.g., current year) if none provided
            var today = DateTime.UtcNow.Date;
            var effectiveStartDate = (parameters.StartDate ?? new DateTime(today.Year, 1, 1)).Date;
            var effectiveEndDate = (parameters.EndDate ?? new DateTime(today.Year, 12, 31)).Date;
            var queryEndDate = effectiveEndDate.AddDays(1); // Exclusive end date for filtering

            // Base query for APPROVED leave requests overlapping the date range
            var leaveQuery = _context.LeaveRequests
                .Include(lr => lr.Employee).ThenInclude(e => e.Team) // Needed for Employee/Team grouping/filtering
                .Include(lr => lr.LeaveType) // Needed for LeaveType grouping/filtering
                .Where(lr => lr.Status == LeaveStatus.Approved)
                // Filter for requests that *overlap* with the requested date range
                .Where(lr => lr.LeaveStartDateTime < queryEndDate && lr.LeaveEndDateTime > effectiveStartDate);

            // Apply optional filters
            if (parameters.EmployeeId.HasValue)
            {
                leaveQuery = leaveQuery.Where(lr => lr.EmployeeId == parameters.EmployeeId.Value);
            }
            if (parameters.TeamId.HasValue)
            {
                leaveQuery = leaveQuery.Where(lr => lr.Employee.TeamId == parameters.TeamId.Value);
            }
            if (parameters.LeaveTypeId.HasValue)
            {
                leaveQuery = leaveQuery.Where(lr => lr.LeaveTypeId == parameters.LeaveTypeId.Value);
            }

            // Fetch the filtered leave requests
            var relevantLeaveRequests = await leaveQuery.ToListAsync();

            // Calculate duration for each request, considering only the part within the query range
            var leaveRequestsWithDuration = relevantLeaveRequests.Select(lr =>
            {
                // Clamp start/end dates to the query range for accurate duration calculation within the period
                var effectiveStart = lr.LeaveStartDateTime > effectiveStartDate ? lr.LeaveStartDateTime : effectiveStartDate;
                var effectiveEnd = lr.LeaveEndDateTime < queryEndDate ? lr.LeaveEndDateTime : queryEndDate;

                // Calculate duration in days (or hours if needed)
                // This simple calculation might need refinement based on business rules (e.g., exclude weekends, consider work hours)
                double durationDays = (effectiveEnd - effectiveStart).TotalDays;
                // Basic example: Round up partial days or use a more sophisticated business day calculator
                durationDays = Math.Max(0, durationDays); // Ensure non-negative

                return new // Anonymous type to hold intermediate results
                {
                    Request = lr, // Keep original request for grouping keys
                    DurationDays = durationDays
                };
            }).ToList();


            // --- Grouping and Aggregation ---
            IEnumerable<LeaveSummaryDto> summaryResult;

            switch (parameters.GroupBy)
            {
                case LeaveSummaryGrouping.LeaveType:
                    summaryResult = leaveRequestsWithDuration
                        .GroupBy(x => x.Request.LeaveType)
                        .Select(g => new LeaveSummaryDto
                        {
                            GroupingDimension = "LeaveType",
                            GroupingId = g.Key.LeaveTypeId, // LeaveType entity is the key
                            GroupingName = g.Key.LeaveTypeName,
                            LeaveRequestCount = g.Count(),
                            TotalLeaveDays = g.Sum(x => x.DurationDays)
                        });
                    break;

                case LeaveSummaryGrouping.Team:
                    summaryResult = leaveRequestsWithDuration
                        .GroupBy(x => x.Request.Employee.Team) // Group by Team entity
                        .Select(g => new LeaveSummaryDto
                        {
                            GroupingDimension = "Team",
                            GroupingId = g.Key?.TeamId, // Handle potential null team
                            GroupingName = g.Key?.TeamName ?? "Unassigned",
                            LeaveRequestCount = g.Count(),
                            TotalLeaveDays = g.Sum(x => x.DurationDays)
                        });
                    break;

                case LeaveSummaryGrouping.Employee:
                    summaryResult = leaveRequestsWithDuration
                       .GroupBy(x => x.Request.Employee) // Group by Employee entity
                       .Select(g => new LeaveSummaryDto
                       {
                           GroupingDimension = "Employee",
                           GroupingId = g.Key.EmployeeId,
                           GroupingName = $"{g.Key.FirstName} {g.Key.LastName}",
                           LeaveRequestCount = g.Count(),
                           TotalLeaveDays = g.Sum(x => x.DurationDays)
                       });
                    break;

                case LeaveSummaryGrouping.None:
                default:
                    // Calculate overall total
                    var totalCount = leaveRequestsWithDuration.Count;
                    var totalDays = leaveRequestsWithDuration.Sum(x => x.DurationDays);
                    summaryResult = new List<LeaveSummaryDto> {
                        new LeaveSummaryDto {
                            GroupingDimension = "Overall",
                            GroupingId = null,
                            GroupingName = "Total",
                            LeaveRequestCount = totalCount,
                            TotalLeaveDays = totalDays
                        }
                    };
                    break;
            }

            return summaryResult.OrderBy(s => s.GroupingName); // Order the final summary
        }
        public async Task<IEnumerable<PendingCountDto>> GetPendingLeaveCountAsync(int? teamId)
        {
            var pendingQuery = _context.LeaveRequests
                .Where(lr => lr.Status == LeaveStatus.Pending);

            if (teamId.HasValue)
            {
                // Filter by team and group by team (even though there's only one team)
                // to easily get the Team Name and ID for the response DTO.
                var teamResult = await pendingQuery
                    .Include(lr => lr.Employee.Team) // Need team info
                    .Where(lr => lr.Employee.TeamId == teamId.Value)
                    .GroupBy(lr => lr.Employee.Team) // Group by the team entity
                    .Select(g => new PendingCountDto
                    {
                        TeamId = g.Key.TeamId, // TeamId from the grouping key
                        TeamName = g.Key.TeamName, // TeamName from the grouping key
                        Count = g.Count() // Count within this team group
                    })
                    .ToListAsync(); // Materialize the result

                // If the specified team exists but has no pending requests, return a count of 0 for that team
                if (!teamResult.Any() && await _context.Teams.AnyAsync(t => t.TeamId == teamId.Value))
                {
                    var team = await _context.Teams.FindAsync(teamId.Value);
                    return new List<PendingCountDto> {
                         new PendingCountDto { TeamId = teamId.Value, TeamName = team?.TeamName ?? "Unknown", Count = 0 }
                     };
                }

                return teamResult;

            }
            else
            {
                // Calculate overall count
                var overallCount = await pendingQuery.CountAsync();
                return new List<PendingCountDto> {
                    new PendingCountDto { TeamId = null, TeamName = "All Teams", Count = overallCount }
                 };
            }
        }
        public async Task<TeamAvailabilityDto?> GetTeamAvailabilityAsync(int teamId, DateTime startDate, DateTime endDate)
        {
            var queryStartDate = startDate.Date;
            var queryEndDate = endDate.Date.AddDays(1); // Exclusive end date

            // 1. Get Team Info and Total Active Member Count
            var team = await _context.Teams
                                     .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return null; // Team not found
            }

            int totalActiveMembers = await _context.Employees
                                            .CountAsync(e => e.TeamId == teamId && e.IsActive);


            // 2. Get Distinct Count of Team Members ON SHIFT during the period
            // Fetches Employee IDs directly
            var onShiftEmployeeIds = await _context.Shifts
                .Where(s => s.Employee.TeamId == teamId // Filter by team
                            && s.Employee.IsActive // Consider only active employees' shifts
                            && s.ShiftStartDateTime < queryEndDate // Shift overlaps period
                            && s.ShiftEndDateTime > queryStartDate)
                .Select(s => s.EmployeeId) // Select only the EmployeeId
                .Distinct() // Get unique employee IDs
                .ToListAsync(); // Execute query

            int membersOnShiftCount = onShiftEmployeeIds.Count;


            // 3. Get Distinct Count of Team Members ON APPROVED LEAVE during the period
            // Fetches Employee IDs directly
            var onLeaveEmployeeIds = await _context.LeaveRequests
                .Where(lr => lr.Employee.TeamId == teamId // Filter by team
                             && lr.Employee.IsActive // Consider only active employees' leave
                             && lr.Status == LeaveStatus.Approved // Only approved leave
                             && lr.LeaveStartDateTime < queryEndDate // Leave overlaps period
                             && lr.LeaveEndDateTime > queryStartDate)
                .Select(lr => lr.EmployeeId) // Select only the EmployeeId
                .Distinct() // Get unique employee IDs
                .ToListAsync(); // Execute query

            int membersOnLeaveCount = onLeaveEmployeeIds.Count;


            // --- Calculate Availability ---
            // This is a simplified calculation. An employee could be both on shift AND on leave
            // during the *same period* if the period is longer than a day and their
            // shift/leave only partially overlaps. This calculation counts them in both totals.
            int potentiallyAvailable = totalActiveMembers - membersOnShiftCount - membersOnLeaveCount;
            // A more accurate 'available' count might require fetching *all* active employee IDs
            // for the team and then subtracting those found in onShiftEmployeeIds and onLeaveEmployeeIds.
            // Example for more accuracy:
            // var activeTeamMemberIds = await _context.Employees
            //                                 .Where(e => e.TeamId == teamId && e.IsActive)
            //                                 .Select(e => e.EmployeeId)
            //                                 .ToListAsync();
            // var unavailableIds = onShiftEmployeeIds.Union(onLeaveEmployeeIds).ToList();
            // var availableIds = activeTeamMemberIds.Except(unavailableIds).ToList();
            // potentiallyAvailable = availableIds.Count;


            var result = new TeamAvailabilityDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                PeriodStart = queryStartDate,
                PeriodEnd = queryEndDate.AddDays(-1), // Make end date inclusive for the DTO
                TotalActiveTeamMembers = totalActiveMembers,
                MembersOnShiftCount = membersOnShiftCount,
                MembersOnLeaveCount = membersOnLeaveCount,
                MembersPotentiallyAvailable = potentiallyAvailable // Use the calculated value
                
            };

            return result;
        }
        public async Task<IEnumerable<LeaveTrendPointDto>> GetLeaveTrendsAsync(LeaveTrendRequestParams parameters)
        {
            // Default date range (e.g., last 12 months) if none provided
            var today = DateTime.UtcNow.Date;
            var effectiveEndDate = (parameters.EndDate ?? today).Date;
            var effectiveStartDate = (parameters.StartDate ?? effectiveEndDate.AddMonths(-12).AddDays(1)).Date; // Default to approx last 12 months
            var queryEndDate = effectiveEndDate.AddDays(1); // Exclusive end

            // Base query for APPROVED leave requests overlapping the date range
            var leaveQuery = _context.LeaveRequests
                .Include(lr => lr.LeaveType) // Needed for potential filtering/grouping
                .Include(lr => lr.Employee)  // Needed for potential filtering
                .Where(lr => lr.Status == LeaveStatus.Approved)
                .Where(lr => lr.LeaveStartDateTime < queryEndDate && lr.LeaveEndDateTime > effectiveStartDate);

            // Apply optional filters
            if (parameters.EmployeeId.HasValue) leaveQuery = leaveQuery.Where(lr => lr.EmployeeId == parameters.EmployeeId.Value);
            if (parameters.TeamId.HasValue) leaveQuery = leaveQuery.Where(lr => lr.Employee.TeamId == parameters.TeamId.Value);
            if (parameters.LeaveTypeId.HasValue) leaveQuery = leaveQuery.Where(lr => lr.LeaveTypeId == parameters.LeaveTypeId.Value);

            var relevantLeaveRequests = await leaveQuery.ToListAsync();

            // Calculate duration within the period for each request
            var leaveRequestsWithDuration = relevantLeaveRequests.Select(lr =>
            {
                var periodStart = lr.LeaveStartDateTime > effectiveStartDate ? lr.LeaveStartDateTime : effectiveStartDate;
                var periodEnd = lr.LeaveEndDateTime < queryEndDate ? lr.LeaveEndDateTime : queryEndDate;
                double durationDays = Math.Max(0, (periodEnd - periodStart).TotalDays);

                return new
                {
                    Request = lr,
                    EffectiveStart = periodStart, // We need the start date for grouping
                    DurationDays = durationDays
                };
            }).ToList();

            // --- Grouping by Time Period ---
            // This requires grouping based on the EffectiveStart date of the leave *within* the period
            IEnumerable<LeaveTrendPointDto> trendResult;

            switch (parameters.Period)
            {
                case TrendPeriod.Monthly:
                    trendResult = leaveRequestsWithDuration
                        .GroupBy(x => new { Year = x.EffectiveStart.Year, Month = x.EffectiveStart.Month }) // Group by Year & Month
                        .Select(g => new LeaveTrendPointDto
                        {
                            PeriodLabel = $"{g.Key.Year}-{g.Key.Month:D2}", // e.g., "2024-01"
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, 1),
                            PeriodEnd = new DateTime(g.Key.Year, g.Key.Month, 1).AddMonths(1).AddDays(-1),
                            LeaveRequestCount = g.Select(x => x.Request.LeaveRequestId).Distinct().Count(), // Count distinct requests starting in period
                            TotalLeaveDays = g.Sum(x => x.DurationDays) // Sum durations calculated earlier
                        })
                        .OrderBy(p => p.PeriodStart);
                    break;

                case TrendPeriod.Quarterly:
                    trendResult = leaveRequestsWithDuration
                        .GroupBy(x => new { Year = x.EffectiveStart.Year, Quarter = ((x.EffectiveStart.Month - 1) / 3) + 1 }) // Group by Year & Quarter
                        .Select(g => new LeaveTrendPointDto
                        {
                            PeriodLabel = $"{g.Key.Year}-Q{g.Key.Quarter}",
                            PeriodStart = new DateTime(g.Key.Year, (g.Key.Quarter - 1) * 3 + 1, 1),
                            PeriodEnd = new DateTime(g.Key.Year, (g.Key.Quarter - 1) * 3 + 1, 1).AddMonths(3).AddDays(-1),
                            LeaveRequestCount = g.Select(x => x.Request.LeaveRequestId).Distinct().Count(),
                            TotalLeaveDays = g.Sum(x => x.DurationDays)
                        })
                        .OrderBy(p => p.PeriodStart);
                    break;

                case TrendPeriod.Yearly:
                    trendResult = leaveRequestsWithDuration
                        .GroupBy(x => new { Year = x.EffectiveStart.Year }) // Group by Year
                        .Select(g => new LeaveTrendPointDto
                        {
                            PeriodLabel = $"{g.Key.Year}",
                            PeriodStart = new DateTime(g.Key.Year, 1, 1),
                            PeriodEnd = new DateTime(g.Key.Year, 12, 31),
                            LeaveRequestCount = g.Select(x => x.Request.LeaveRequestId).Distinct().Count(),
                            TotalLeaveDays = g.Sum(x => x.DurationDays)
                        })
                        .OrderBy(p => p.PeriodStart);
                    break;

                default: // Should not happen with enum, but handle defensively
                    trendResult = Enumerable.Empty<LeaveTrendPointDto>();
                    break;
            }

            // Optional: Fill in missing periods with zero values if needed for charting
            // Requires generating all expected periods in the range and joining with the results.

            return trendResult;
        }
        // Add to Services/DashboardService.cs
        public async Task<IEnumerable<ShiftTypeDistributionDto>> GetShiftTypeDistributionAsync(
            DateTime startDate, DateTime endDate, int? teamId)
        {
            var queryStartDate = startDate.Date;
            var queryEndDate = endDate.Date.AddDays(1);

            var shiftsQuery = _context.Shifts
                .Include(s => s.ShiftType) // Need ShiftType info
                .Include(s => s.Employee) // Need Employee for Team filter
                .Where(s => s.ShiftType != null) // Exclude shifts without a type if any exist
                .Where(s => s.ShiftStartDateTime < queryEndDate && s.ShiftEndDateTime > queryStartDate); // Overlap filter

            if (teamId.HasValue)
            {
                shiftsQuery = shiftsQuery.Where(s => s.Employee.TeamId == teamId.Value);
            }

            // Group by ShiftType and count
            var distributionCounts = await shiftsQuery
                .GroupBy(s => s.ShiftType) // Group by the ShiftType entity
                .Select(g => new
                {
                    ShiftTypeId = g.Key.ShiftTypeId, // Key is the ShiftType
                    ShiftTypeName = g.Key.TypeName,
                    IsOnCall = g.Key.IsOnCall,
                    ShiftCount = g.Count()
                })
                .OrderBy(x => x.ShiftTypeName)
                .ToListAsync();

            // Calculate percentages
            long totalShifts = distributionCounts.Sum(d => d.ShiftCount); // Use long for sum if counts can be very large
            if (totalShifts == 0) totalShifts = 1; // Avoid division by zero

            var result = distributionCounts.Select(d => new ShiftTypeDistributionDto
            {
                ShiftTypeId = d.ShiftTypeId,
                ShiftTypeName = d.ShiftTypeName,
                IsOnCall = d.IsOnCall,
                ShiftCount = d.ShiftCount,
                PercentageOfTotal = Math.Round((double)d.ShiftCount * 100 / totalShifts, 2) // Calculate percentage
            }).ToList();

            return result;
        }
        // Add to Services/DashboardService.cs
        public async Task<IEnumerable<ScheduleItemDto>> GetEmployeeScheduleAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            var queryStartDate = startDate.Date;
            var queryEndDate = endDate.Date.AddDays(1);

            // 1. Get Shifts
            var shifts = await _context.Shifts
                .Include(s => s.ShiftType)
                .Where(s => s.EmployeeId == employeeId)
                .Where(s => s.ShiftStartDateTime < queryEndDate && s.ShiftEndDateTime > queryStartDate)
                .Select(s => new ScheduleItemDto
                {
                    ItemType = "Shift",
                    StartDateTime = s.ShiftStartDateTime,
                    EndDateTime = s.ShiftEndDateTime,
                    Description = s.ShiftType != null ? s.ShiftType.TypeName : "Unknown Type",
                    Notes = s.Notes,
                    ReferenceId = s.ShiftId,
                    Status = null // Shifts don't have status like leave
                })
                .ToListAsync();

            // 2. Get Leave Requests (Approved or Pending might be relevant)
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.EmployeeId == employeeId)
                .Where(lr => lr.Status == LeaveStatus.Approved || lr.Status == LeaveStatus.Pending) // Include pending? Or just approved?
                .Where(lr => lr.LeaveStartDateTime < queryEndDate && lr.LeaveEndDateTime > queryStartDate)
                .Select(lr => new ScheduleItemDto
                {
                    ItemType = "Leave",
                    StartDateTime = lr.LeaveStartDateTime,
                    EndDateTime = lr.LeaveEndDateTime,
                    Description = lr.LeaveType.LeaveTypeName,
                    Notes = lr.Reason, // Use Reason as notes?
                    ReferenceId = lr.LeaveRequestId,
                    Status = lr.Status.ToString()
                })
                .ToListAsync();

            // 3. Combine and Sort
            var schedule = shifts.Concat(leaveRequests)
                                 .OrderBy(item => item.StartDateTime)
                                 .ToList();

            return schedule;
        }
        public async Task<IEnumerable<OnCallGapDto>> GetOnCallGapsAsync(int requiredShiftTypeId, DateTime startDate, DateTime endDate)
        {
            var queryStartDate = startDate.Date;
            var queryEndDate = endDate.Date.AddDays(1); // Exclusive

            var requiredShiftType = await _context.ShiftTypes.FindAsync(requiredShiftTypeId);
            if (requiredShiftType == null || !requiredShiftType.IsOnCall)
            {
                // Or throw ArgumentException if the type isn't valid for this analysis
                return Enumerable.Empty<OnCallGapDto>();
            }

            // Get all shifts of the required type overlapping the period, ordered by start time
            var assignedShifts = await _context.Shifts
                .Where(s => s.ShiftTypeId == requiredShiftTypeId)
                .Where(s => s.ShiftStartDateTime < queryEndDate && s.ShiftEndDateTime > queryStartDate)
                .OrderBy(s => s.ShiftStartDateTime)
                .Select(s => new { s.ShiftStartDateTime, s.ShiftEndDateTime }) // Select only needed fields
                .ToListAsync();

            var gaps = new List<OnCallGapDto>();
            DateTime currentPointer = queryStartDate; // Start tracking from the beginning of the query range

            foreach (var shift in assignedShifts)
            {
                // Clamp shift start/end to the query window for gap calculation within the window
                var effectiveShiftStart = shift.ShiftStartDateTime < queryStartDate ? queryStartDate : shift.ShiftStartDateTime;
                var effectiveShiftEnd = shift.ShiftEndDateTime > queryEndDate ? queryEndDate : shift.ShiftEndDateTime;

                // Is there a gap between the current pointer and the start of this shift?
                if (effectiveShiftStart > currentPointer)
                {
                    // Report the gap
                    gaps.Add(new OnCallGapDto
                    {
                        RequiredShiftTypeId = requiredShiftTypeId,
                        RequiredShiftTypeName = requiredShiftType.TypeName,
                        GapStart = currentPointer,
                        GapEnd = effectiveShiftStart, // The gap ends when the shift starts
                        DurationHours = (effectiveShiftStart - currentPointer).TotalHours
                    });
                }

                // Move the pointer to the end of the current shift (or the latest end if shifts overlap)
                currentPointer = currentPointer > effectiveShiftEnd ? currentPointer : effectiveShiftEnd;
            }

            // Check for a final gap between the end of the last shift and the end of the query period
            if (currentPointer < queryEndDate)
            {
                gaps.Add(new OnCallGapDto
                {
                    RequiredShiftTypeId = requiredShiftTypeId,
                    RequiredShiftTypeName = requiredShiftType.TypeName,
                    GapStart = currentPointer,
                    GapEnd = queryEndDate, // Gap ends at the end of the query period
                    DurationHours = (queryEndDate - currentPointer).TotalHours
                });
            }

            // Filter out very small gaps if desired (e.g., less than 5 minutes)
            // return gaps.Where(g => g.DurationHours > (5.0/60.0));
            return gaps;
        }
    }
}
