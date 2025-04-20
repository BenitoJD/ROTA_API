using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public EmployeeService(RotaDbContext context, IMapper mapper) 
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
        {
            return await _context.Employees
                .Include(e => e.Team) // Include for mapping TeamName
                .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                // ProjectTo is often more efficient for read operations than mapping after ToListAsync
                .ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Team) 
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return null; // Indicate not found
            }

            return _mapper.Map<EmployeeDto>(employee);
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto)
        {
            if (createDto.TeamId.HasValue && !await _context.Teams.AnyAsync(t => t.TeamId == createDto.TeamId.Value))
            {
                throw new ArgumentException($"Specified Team ID {createDto.TeamId.Value} does not exist.", nameof(CreateEmployeeDto.TeamId));
            }

            var employee = _mapper.Map<Employee>(createDto);

            employee.CreatedAt = DateTime.UtcNow;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.IsActive = true;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            
            var createdEmployeeWithDetails = await _context.Employees
                                                    .Include(e => e.Team)
                                                    .FirstAsync(e => e.EmployeeId == employee.EmployeeId); // Use the generated ID


            return _mapper.Map<EmployeeDto>(createdEmployeeWithDetails); // Return the DTO of the created employee
        }

        public async Task<bool> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto)
        {
            var employeeToUpdate = await _context.Employees.FindAsync(id);

            if (employeeToUpdate == null)
            {
                return false; 
            }

            if (updateDto.TeamId.HasValue && updateDto.TeamId != employeeToUpdate.TeamId)
            {
                if (!await _context.Teams.AnyAsync(t => t.TeamId == updateDto.TeamId.Value))
                {
                    throw new ArgumentException($"Team with ID {updateDto.TeamId.Value} does not exist.", nameof(UpdateEmployeeDto.TeamId));
                }
            }

            _mapper.Map(updateDto, employeeToUpdate); 
            employeeToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return true; 
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Employees.AnyAsync(e => e.EmployeeId == id)) return false; // Was deleted
                else throw; // Or handle differently
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employeeToDelete = await _context.Employees.FindAsync(id);

            if (employeeToDelete == null)
            {
                return false; // Indicate not found
            }

            _context.Employees.Remove(employeeToDelete);

            try
            {
                await _context.SaveChangesAsync();
                return true; // Indicate success
            }
            catch (DbUpdateException ex) // Catch potential FK constraint issues if cascade delete fails somehow
            {
                // Log the error ex
                Console.WriteLine($"Error deleting employee {id} in service: {ex}");
                throw;
            }
        }
    }
}
