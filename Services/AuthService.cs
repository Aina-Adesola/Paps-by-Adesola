using Microsoft.AspNetCore.Identity;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Services;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Mappers;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;          // NEW: replaces the OTP columns in the database
        private readonly ICurrentUserService _currentUser;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuditService auditService,
            ITokenService tokenService,
            IOtpService otpService,
            ICurrentUserService currentUser)
        {
            _userManager = userManager;
            _emailService = emailService;
            _auditService = auditService;
            _tokenService = tokenService;
            _otpService = otpService;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<object>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse.Fail("Invalid email or password.", 401);

            if (await _userManager.IsLockedOutAsync(user))
                return ApiResponse.Fail("This account has been disabled.", 401);

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return ApiResponse.Fail("Invalid email or password.", 401);

            // The code is created and kept in the cache. Nothing is saved to the database now.
            var otp = _otpService.Generate(user.Id);

            var sent = await _emailService.SendOtpEmailAsync(user.Email!, otp);
            if (!sent)
            {
                _otpService.Remove(user.Id); // no point keeping a code the user never received
                return ApiResponse.Fail(
                    "We couldn't send your verification code right now. Please try again in a moment.", 503);
            }

            return ApiResponse.Ok(
                "A verification code has been sent to your email. Call /api/auth/verify-otp to finish logging in.");
        }

        public async Task<ApiResponse<LoginResponseDto>> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse.Fail<LoginResponseDto>("Invalid email or code.", 401);

            // An account disabled between step 1 and step 2 must not get in.
            if (await _userManager.IsLockedOutAsync(user))
                return ApiResponse.Fail<LoginResponseDto>("This account has been disabled.", 401);

            switch (_otpService.Verify(user.Id, dto.Otp))
            {
                case OtpResult.Expired:
                    return ApiResponse.Fail<LoginResponseDto>(
                        "No active code, or it has expired. Please log in again to request a new one.", 400);

                case OtpResult.TooManyAttempts:
                    return ApiResponse.Fail<LoginResponseDto>(
                        "Too many wrong codes. Please log in again to request a new one.", 429);

                case OtpResult.Invalid:
                    return ApiResponse.Fail<LoginResponseDto>("Invalid email or code.", 401);
            }

            // ---- the code was correct ----
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var displayName = GetDisplayName(user);

            var sessionId = await _auditService.StartSessionAsync(user.Id, displayName);
            await _auditService.LogAsync(sessionId, user.Id, displayName, "Login");

            var token = _tokenService.GenerateToken(user, roles, sessionId);

            var data = new LoginResponseDto { Token = token, User = user.ToDto(roles) };
            return ApiResponse.Ok(data, "Login successful.");
        }

        public async Task<ApiResponse<object>> LogoutAsync()
        {
            if (_currentUser.SessionId is int sessionId)
            {
                await _auditService.EndSessionAsync(sessionId);
                await _auditService.LogAsync(
                    sessionId, _currentUser.UserId, _currentUser.DisplayName, "Logout");
            }

            return ApiResponse.Ok("Logged out.");
        }

        private static string GetDisplayName(ApplicationUser user)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : (user.Email ?? "Unknown");
        }
    }
}