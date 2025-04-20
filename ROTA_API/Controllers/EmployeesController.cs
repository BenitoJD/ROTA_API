using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;
using ROTA_API.Models;
using ROTA_API.Services;

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }


        // GET: api/employees
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmployeeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employeeDtos = await _employeeService.GetAllEmployeesAsync();
            return Ok(employeeDtos); // Service returns the DTOs directly
        }

        // GET: api/employees/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
        {
            var employeeDto = await _employeeService.GetEmployeeByIdAsync(id);

            if (employeeDto == null)
            {
                return NotFound($"Employee with ID {id} not found.");
            }

            return Ok(employeeDto);
        }

        // POST: api/employees
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Handle potential exceptions from service
        public async Task<ActionResult<EmployeeDto>> PostEmployee([FromBody] CreateEmployeeDto createEmployeeDto)
        {
            try
            {
                var createdEmployeeDto = await _employeeService.CreateEmployeeAsync(createEmployeeDto);
                // Service returns the DTO of the created employee
                return CreatedAtAction(nameof(GetEmployee), new { id = createdEmployeeDto.EmployeeId }, createdEmployeeDto);
            }
            catch (ArgumentException ex) // Catch validation errors thrown by service
            {
                // Add error to model state for consistent BadRequest response
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex) // Catch unexpected errors
            {
                // Log the exception ex
                Console.WriteLine($"Unexpected error creating employee: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        // PUT: api/employees/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Handle potential exceptions from service
        public async Task<IActionResult> PutEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        {
            try
            {
                bool success = await _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto);

                if (!success)
                {
                    return NotFound($"Employee with ID {id} not found for update."); // Service indicated not found
                }

                return NoContent(); // Success
            }
            catch (ArgumentException ex) // Catch validation errors thrown by service (like invalid TeamId)
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (DbUpdateConcurrencyException) // Could still happen if service re-throws
            {
                // Log or handle specifically
                return StatusCode(StatusCodes.Status409Conflict, "The employee record was modified by another user.");
            }
            catch (Exception ex) // Catch unexpected errors
            {
                // Log the exception ex
                Console.WriteLine($"Unexpected error updating employee {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }


        // DELETE: api/employees/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                bool success = await _employeeService.DeleteEmployeeAsync(id);

                if (!success)
                {
                    return NotFound($"Employee with ID {id} not found for deletion.");
                }

                return NoContent(); // Success
            }
            catch (Exception ex) // Catch unexpected errors (like DB errors re-thrown by service)
            {
                // Log the exception ex
                Console.WriteLine($"Unexpected error deleting employee {id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }

        }
    }
}
