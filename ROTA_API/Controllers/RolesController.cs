using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ROTA_API.Data;
using ROTA_API.DTOs;

namespace ROTA_API.Controllers
{
    [Route("api/[controller]")] // Route: /api/roles
    [ApiController]
    [Authorize(Roles = "Admin")] // IMPORTANT: Only Admins should manage/list roles typically
    public class RolesController : ControllerBase
    {
        private readonly RotaDbContext _context;
        private readonly IMapper _mapper;

        public RolesController(RotaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/roles
        /// <summary>
        /// Retrieves a list of all available roles. (Admin Only)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _context.Roles
                                    .OrderBy(r => r.RoleName)
                                    .ProjectTo<RoleDto>(_mapper.ConfigurationProvider) // Use projection
                                    .ToListAsync();

            // Alternative: Manual mapping
            // var roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
            // var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            // return Ok(roleDtos);

            return Ok(roles);
        }
    }
}