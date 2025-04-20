using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public LeaveRequestService(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private async Task<int?> GetEmployeeIdForUserIdAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => (int?)u.EmployeeId)
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsAsync(
            int requestingUserId, bool isRequestingUserAdmin,
            int? employeeId, LeaveStatus? status, DateTime? startDate, DateTime? endDate, int? leaveTypeId, int? teamId)
        {
            var query = _context.LeaveRequests
                .Include(lr => lr.Employee).ThenInclude(e => e.Team)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.ApproverUser)
                .AsQueryable();

            if (!isRequestingUserAdmin)
            {
                var requestingUsersEmployeeId = await GetEmployeeIdForUserIdAsync(requestingUserId);
                if (requestingUsersEmployeeId == null)
                {
                    // Or throw an exception if user MUST be linked
                    return Enumerable.Empty<LeaveRequestDto>(); // Return empty if user not linked
                }

                // If filtering by specific employee, ensure it's the current user
                if (employeeId.HasValue && employeeId.Value != requestingUsersEmployeeId.Value)
                {
                    return Enumerable.Empty<LeaveRequestDto>(); // Trying to access someone else's
                }
                // If no employee filter, restrict to own requests
                else if (!employeeId.HasValue)
                {
                    query = query.Where(lr => lr.EmployeeId == requestingUsersEmployeeId.Value);
                }
                // If employeeId filter IS applied and matches, the filter below handles it
            }


            if (employeeId.HasValue) query = query.Where(lr => lr.EmployeeId == employeeId.Value);
            if (teamId.HasValue) query = query.Where(lr => lr.Employee.TeamId == teamId.Value);
            if (status.HasValue) query = query.Where(lr => lr.Status == status.Value);
            if (leaveTypeId.HasValue) query = query.Where(lr => lr.LeaveTypeId == leaveTypeId.Value);
            if (startDate.HasValue && endDate.HasValue) query = query.Where(lr => lr.LeaveStartDateTime < endDate.Value.AddDays(1) && lr.LeaveEndDateTime > startDate.Value);

            return await query
                        .OrderByDescending(lr => lr.RequestedDate)
                        .ProjectTo<LeaveRequestDto>(_mapper.ConfigurationProvider)
                        .ToListAsync();
        }

        public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id, int requestingUserId, bool isRequestingUserAdmin)
        {
            var leaveRequestQuery = _context.LeaveRequests
               .Where(lr => lr.LeaveRequestId == id)
               // Includes needed for ProjectTo mapping
               .Include(lr => lr.Employee).ThenInclude(e => e.Team)
               .Include(lr => lr.LeaveType)
               .Include(lr => lr.ApproverUser)
               .ProjectTo<LeaveRequestDto>(_mapper.ConfigurationProvider);

            var leaveRequestDto = await leaveRequestQuery.FirstOrDefaultAsync();

            if (leaveRequestDto == null) return null; // Not found

            // Authorization Check
            if (!isRequestingUserAdmin)
            {
                var requestingUsersEmployeeId = await GetEmployeeIdForUserIdAsync(requestingUserId);
                if (requestingUsersEmployeeId == null || leaveRequestDto.EmployeeId != requestingUsersEmployeeId.Value)
                {
                    return null; // Forbidden, treat as not found for this user
                }
            }

            return leaveRequestDto;
        }

        public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createDto, int creatorUserId, bool isCreatorAdmin)
        {
            int employeeIdForRequest = createDto.EmployeeId;

            // Authorization Check
            if (!isCreatorAdmin)
            {
                var creatorEmployeeId = await GetEmployeeIdForUserIdAsync(creatorUserId);
                if (creatorEmployeeId == null) throw new UnauthorizedAccessException("User is not linked to an employee record.");
                if (employeeIdForRequest != creatorEmployeeId.Value) throw new UnauthorizedAccessException("You can only create leave requests for yourself.");
            }

            // Validation
            var employeeExists = await _context.Employees.AnyAsync(e => e.EmployeeId == employeeIdForRequest && e.IsActive);
            if (!employeeExists) throw new ArgumentException($"Active employee with ID {employeeIdForRequest} not found.", nameof(createDto.EmployeeId));

            var leaveTypeExists = await _context.LeaveTypes.AnyAsync(lt => lt.LeaveTypeId == createDto.LeaveTypeId);
            if (!leaveTypeExists) throw new ArgumentException($"LeaveType with ID {createDto.LeaveTypeId} not found.", nameof(createDto.LeaveTypeId));

            if (createDto.LeaveStartDateTime >= createDto.LeaveEndDateTime) throw new ArgumentException("Leave end date/time must be after the start date/time.", nameof(createDto.LeaveEndDateTime));

            var overlappingApprovedLeave = await _context.LeaveRequests.AnyAsync(lr => lr.EmployeeId == employeeIdForRequest && lr.Status == LeaveStatus.Approved && lr.LeaveStartDateTime < createDto.LeaveEndDateTime && lr.LeaveEndDateTime > createDto.LeaveStartDateTime);
            if (overlappingApprovedLeave) throw new InvalidOperationException("Employee already has approved leave during this time period.");

            // Handle shift conflict (optional - maybe just log or return warning in DTO?)
            bool conflictsWithShift = await _context.Shifts.AnyAsync(s => s.EmployeeId == employeeIdForRequest && s.ShiftStartDateTime < createDto.LeaveEndDateTime && s.ShiftEndDateTime > createDto.LeaveStartDateTime);
            if (conflictsWithShift) Console.WriteLine($"Warning: Creating leave request for employee {employeeIdForRequest} that conflicts with a shift.");


            var leaveRequest = _mapper.Map<LeaveRequest>(createDto);

            leaveRequest.Status = LeaveStatus.Pending;
            leaveRequest.RequestedDate = DateTime.UtcNow;
            leaveRequest.CreatedAt = DateTime.UtcNow;
            leaveRequest.UpdatedAt = DateTime.UtcNow;
            leaveRequest.CreatedByUserId = creatorUserId;
            leaveRequest.UpdatedByUserId = creatorUserId;

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            // Fetch DTO efficiently after save
            var createdDto = await _context.LeaveRequests
                                .Where(lr => lr.LeaveRequestId == leaveRequest.LeaveRequestId)
                                .ProjectTo<LeaveRequestDto>(_mapper.ConfigurationProvider)
                                .FirstAsync(); // Should exist

            return createdDto;
        }

        public async Task<LeaveRequestDto?> UpdateLeaveRequestStatusAsync(int id, UpdateLeaveStatusDto statusDto, int approverUserId)
        {
            // Only Admins can call this, so no need for isApproverAdmin param, assume true

            if (statusDto.NewStatus != LeaveStatus.Approved && statusDto.NewStatus != LeaveStatus.Rejected)
            {
                throw new ArgumentException("Status can only be updated to 'Approved' or 'Rejected'.", nameof(statusDto.NewStatus));
            }

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null) return null; // Not found

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                throw new InvalidOperationException($"Leave request must be in 'Pending' status to be approved or rejected. Current status: {leaveRequest.Status}.");
            }

            // Optional: Conflict check before approving
            if (statusDto.NewStatus == LeaveStatus.Approved)
            {
                var overlappingApprovedLeave = await _context.LeaveRequests.AnyAsync(lr => lr.LeaveRequestId != id && lr.EmployeeId == leaveRequest.EmployeeId && lr.Status == LeaveStatus.Approved && lr.LeaveStartDateTime < leaveRequest.LeaveEndDateTime && lr.LeaveEndDateTime > leaveRequest.LeaveStartDateTime);
                if (overlappingApprovedLeave) throw new InvalidOperationException("Cannot approve: Employee already has overlapping approved leave during this time period.");
            }

            leaveRequest.Status = statusDto.NewStatus;
            leaveRequest.ApproverNotes = statusDto.ApproverNotes;
            leaveRequest.ApproverUserId = approverUserId;
            leaveRequest.ApprovalDate = DateTime.UtcNow;
            leaveRequest.UpdatedAt = DateTime.UtcNow;
            leaveRequest.UpdatedByUserId = approverUserId;

            await _context.SaveChangesAsync();

            // Fetch updated DTO
            return await _context.LeaveRequests
                        .Where(lr => lr.LeaveRequestId == id)
                        .ProjectTo<LeaveRequestDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync(); // Use FirstOrDefault just in case
        }

        public async Task<bool> CancelLeaveRequestAsync(int id, int cancellerUserId, bool isCancellerAdmin)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null) return false; // Not found

            if (leaveRequest.Status != LeaveStatus.Pending && leaveRequest.Status != LeaveStatus.Approved)
            {
                // Cannot cancel if already rejected/cancelled
                return false; // Or throw InvalidOperationException
            }



            // Authorization check
            bool canCancel = false;
            if (isCancellerAdmin)
            {
                canCancel = true;
            }
            else
            {
                var cancellerEmployeeId = await GetEmployeeIdForUserIdAsync(cancellerUserId);
                if (cancellerEmployeeId != null && leaveRequest.EmployeeId == cancellerEmployeeId.Value)
                {
                    canCancel = true;
                }
            }

            if (!canCancel)
            {
                throw new UnauthorizedAccessException("User does not have permission to cancel this leave request.");
            }

            leaveRequest.Status = LeaveStatus.Cancelled;
            leaveRequest.UpdatedAt = DateTime.UtcNow;
            leaveRequest.UpdatedByUserId = cancellerUserId;

            // Optional: Clear approval details
            if (leaveRequest.ApprovalDate != null)
            {
                leaveRequest.ApproverUserId = null;
                leaveRequest.ApprovalDate = null;
                leaveRequest.ApproverNotes = (leaveRequest.ApproverNotes ?? "") + " [Cancelled after approval]";
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DoesLeaveRequestExistAsync(int id)
        {
            return await _context.LeaveRequests.AnyAsync(lr => lr.LeaveRequestId == id);
        }
    }
}
