using ROTA_API.DTOs;

namespace ROTA_API.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id); // Nullable DTO indicates not found
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto); // Returns created DTO
        Task<bool> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto); // Returns true if updated, false if not found
        Task<bool> DeleteEmployeeAsync(int id); // Returns true if deleted, false if not found
    }
}
