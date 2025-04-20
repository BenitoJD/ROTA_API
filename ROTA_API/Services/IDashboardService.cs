using ROTA_API.DTOs;

namespace ROTA_API.Services
{
    public interface IDashboardService
    {
        Task<IEnumerable<UpcomingOnCallDto>> GetUpcomingOnCallAsync(
            DateTime startDate,
            DateTime endDate,
            int? teamId);

        Task<IEnumerable<ShiftCoverageDto>> GetShiftCoverageAsync(
                    DateTime startDate,
                    DateTime endDate,
                    int? teamId,
                    int? shiftTypeId,
                    bool groupByTeam); // Flag to indicate if results should be per-team or overall
        Task<IEnumerable<LeaveSummaryDto>> GetLeaveSummaryAsync(LeaveSummaryRequestParams parameters);
        Task<IEnumerable<PendingCountDto>> GetPendingLeaveCountAsync(int? teamId);
        Task<TeamAvailabilityDto?> GetTeamAvailabilityAsync(int teamId, DateTime startDate, DateTime endDate); // Nullable if team not found


        Task<IEnumerable<LeaveTrendPointDto>> GetLeaveTrendsAsync(LeaveTrendRequestParams parameters);
        Task<IEnumerable<ShiftTypeDistributionDto>> GetShiftTypeDistributionAsync(
                                                    DateTime startDate,
                                                    DateTime endDate,
                                                    int? teamId);
        Task<IEnumerable<ScheduleItemDto>> GetEmployeeScheduleAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<OnCallGapDto>> GetOnCallGapsAsync(int requiredShiftTypeId, DateTime startDate, DateTime endDate);
    }
}
