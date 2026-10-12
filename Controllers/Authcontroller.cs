using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Services;

namespace PAPPSADMIN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
            => FromResponse(await _authService.LoginAsync(dto));

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
            => FromResponse(await _authService.VerifyOtpAsync(dto));

        // Note: this does NOT invalidate the JWT itself - JWTs are stateless.
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
            => FromResponse(await _authService.LogoutAsync());
    }
}
