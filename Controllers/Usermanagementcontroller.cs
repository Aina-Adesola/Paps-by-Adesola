using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;
using PAPPSADMIN.Services;

namespace PAPPSADMIN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.SGC)] // Every endpoint here requires the SGC role
    public class UserManagementController : ApiControllerBase
    {
        private readonly IUserManagementService _userService;

        public UserManagementController(IUserManagementService userService)
        {
            _userService = userService;
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
            => FromResponse(await _userService.CreateUserAsync(dto));

        // GET api/usermanagement - every user, across all roles.
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
            => FromResponse(await _userService.GetAllUsersAsync());

        // GET api/usermanagement/{staffId}
        [HttpGet("{staffId}")]
        public async Task<IActionResult> GetUserByStaffId(string staffId)
            => FromResponse(await _userService.GetUserByStaffIdAsync(staffId));

        [HttpPut("{id:int}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UpdateUserDto dto)
            => FromResponse(await _userService.EditUserAsync(id, dto));

        [HttpPost("{id:int}/disable")]
        public async Task<IActionResult> DisableUser(int id)
            => FromResponse(await _userService.DisableUserAsync(id));

        [HttpPost("{id:int}/enable")]
        public async Task<IActionResult> EnableUser(int id)
            => FromResponse(await _userService.EnableUserAsync(id));
    }
}
