using ROTA_API.DTOs;

namespace ROTA_API.Services
{
    public interface IShiftService
    {
        // Define parameters for filtering directly in the service method
        Task<IEnumerable<ShiftDto>> GetShiftsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? employeeId,
            int? teamId,
            bool? isOnCall);

        Task<ShiftDto?> GetShiftByIdAsync(int id);

        // Need the ID of the user performing the action for auditing
        Task<ShiftDto> CreateShiftAsync(CreateShiftDto createDto, int creatorUserId);

        Task<bool> UpdateShiftAsync(int id, UpdateShiftDto updateDto, int updaterUserId);

        Task<bool> DeleteShiftAsync(int id);
    }
}
