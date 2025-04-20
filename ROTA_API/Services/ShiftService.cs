using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API.Services
{
    public class ShiftService : IShiftService
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public ShiftService(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ShiftDto>> GetShiftsAsync(DateTime? startDate, DateTime? endDate, int? employeeId, int? teamId, bool? isOnCall)
        {
            var query = _context.Shifts
                                .Include(s => s.Employee).ThenInclude(e => e.Team)
                                .Include(s => s.ShiftType)
                                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue) // Ensure both are provided for range filtering
            {
                query = query.Where(s => s.ShiftStartDateTime < endDate.Value.AddDays(1) && s.ShiftEndDateTime > startDate.Value);
            }
            if (employeeId.HasValue)
            {
                query = query.Where(s => s.EmployeeId == employeeId.Value);
            }
            if (teamId.HasValue)
            {
                query = query.Where(s => s.Employee.TeamId == teamId.Value);
            }
            if (isOnCall.HasValue)
            {
                query = query.Where(s => s.ShiftType != null && s.ShiftType.IsOnCall == isOnCall.Value);
            }

            return await query
                        .OrderBy(s => s.ShiftStartDateTime).ThenBy(s => s.Employee.LastName)
                        .ProjectTo<ShiftDto>(_mapper.ConfigurationProvider)
                        .ToListAsync();
        }

        public async Task<ShiftDto?> GetShiftByIdAsync(int id)
        {
            // ProjectTo is efficient here too
            return await _context.Shifts
                        .Where(s => s.ShiftId == id)
                        .Include(s => s.Employee).ThenInclude(e => e.Team) // Still need includes if ProjectTo relies on them
                        .Include(s => s.ShiftType)
                        .ProjectTo<ShiftDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync();

          
        }

        public async Task<ShiftDto> CreateShiftAsync(CreateShiftDto createDto, int creatorUserId)
        {
            // --- Validation within the service ---
            var employeeExists = await _context.Employees.AnyAsync(e => e.EmployeeId == createDto.EmployeeId && e.IsActive);
            if (!employeeExists) throw new ArgumentException($"Active employee with ID {createDto.EmployeeId} not found.", nameof(createDto.EmployeeId));

            if (createDto.ShiftTypeId.HasValue && !await _context.ShiftTypes.AnyAsync(st => st.ShiftTypeId == createDto.ShiftTypeId.Value))
            {
                throw new ArgumentException($"ShiftType with ID {createDto.ShiftTypeId.Value} not found.", nameof(createDto.ShiftTypeId));
            }

            if (createDto.ShiftStartDateTime >= createDto.ShiftEndDateTime)
            {
                throw new ArgumentException("Shift end date/time must be after the start date/time.", nameof(createDto.ShiftEndDateTime));
            }

            var overlappingShift = await _context.Shifts.AnyAsync(s =>
                s.EmployeeId == createDto.EmployeeId &&
                s.ShiftStartDateTime < createDto.ShiftEndDateTime &&
                s.ShiftEndDateTime > createDto.ShiftStartDateTime);

            if (overlappingShift)
            {
                // Using a more specific exception type might be better
                throw new InvalidOperationException($"Employee already has an overlapping shift during this time period.");
            }

            var shift = _mapper.Map<Shift>(createDto);

            shift.CreatedAt = DateTime.UtcNow;
            shift.UpdatedAt = DateTime.UtcNow;
            shift.CreatedByUserId = creatorUserId;
            shift.UpdatedByUserId = creatorUserId;

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            
            var createdDto = await _context.Shifts
                                    .Where(s => s.ShiftId == shift.ShiftId)
                                    .ProjectTo<ShiftDto>(_mapper.ConfigurationProvider)
                                    .FirstAsync(); 

            return createdDto;
        }

        public async Task<bool> UpdateShiftAsync(int id, UpdateShiftDto updateDto, int updaterUserId)
        {
            var shiftToUpdate = await _context.Shifts.FindAsync(id);
            if (shiftToUpdate == null) return false; // Not found

            if (updateDto.EmployeeId != shiftToUpdate.EmployeeId && !await _context.Employees.AnyAsync(e => e.EmployeeId == updateDto.EmployeeId && e.IsActive))
            {
                throw new ArgumentException($"Active employee with ID {updateDto.EmployeeId} not found.", nameof(updateDto.EmployeeId));
            }
            if (updateDto.ShiftTypeId.HasValue && updateDto.ShiftTypeId != shiftToUpdate.ShiftTypeId && !await _context.ShiftTypes.AnyAsync(st => st.ShiftTypeId == updateDto.ShiftTypeId.Value))
            {
                throw new ArgumentException($"ShiftType with ID {updateDto.ShiftTypeId.Value} not found.", nameof(updateDto.ShiftTypeId));
            }
            if (updateDto.ShiftStartDateTime >= updateDto.ShiftEndDateTime)
            {
                throw new ArgumentException("Shift end date/time must be after the start date/time.", nameof(updateDto.ShiftEndDateTime));
            }
            var overlappingShift = await _context.Shifts.AnyAsync(s =>
                s.ShiftId != id &&
                s.EmployeeId == updateDto.EmployeeId &&
                s.ShiftStartDateTime < updateDto.ShiftEndDateTime &&
                s.ShiftEndDateTime > updateDto.ShiftStartDateTime);
            if (overlappingShift)
            {
                throw new InvalidOperationException($"The assigned employee already has an overlapping shift during this time period.");
            }

            _mapper.Map(updateDto, shiftToUpdate); // Apply changes

            shiftToUpdate.UpdatedAt = DateTime.UtcNow;
            shiftToUpdate.UpdatedByUserId = updaterUserId;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Shifts.AnyAsync(e => e.ShiftId == id)) return false; // Was deleted
                else throw; // Re-throw for controller to handle as 409 Conflict maybe
            }
        }

        public async Task<bool> DeleteShiftAsync(int id)
        {
            var shiftToDelete = await _context.Shifts.FindAsync(id);
            if (shiftToDelete == null) return false; // Not found

            _context.Shifts.Remove(shiftToDelete);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Log ex
                Console.WriteLine($"Error deleting shift {id} in service: {ex}");
                // Re-throw for controller to handle as 500 potentially
                throw;
            }
        }
    }
}
