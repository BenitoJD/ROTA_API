using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Services
{
    public interface ILeaveRequestService
    {
        Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsAsync(
            int requestingUserId, // ID of the user making the request
            bool isRequestingUserAdmin, // Is the requesting user an admin?
            int? employeeId,
            LeaveStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            int? leaveTypeId,
            int? teamId);

        Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id, int requestingUserId, bool isRequestingUserAdmin);

        Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createDto, int creatorUserId, bool isCreatorAdmin);

        Task<LeaveRequestDto?> UpdateLeaveRequestStatusAsync(int id, UpdateLeaveStatusDto statusDto, int approverUserId); // Return updated DTO or null if failed/not found

        Task<bool> CancelLeaveRequestAsync(int id, int cancellerUserId, bool isCancellerAdmin); // Return true/false for success/failure

        Task<bool> DoesLeaveRequestExistAsync(int id); // Check if a leave request exists by ID
    }
}
