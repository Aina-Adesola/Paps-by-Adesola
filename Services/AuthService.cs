using Microsoft.AspNetCore.Identity;
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
        private readonly IOtpService _otpService;
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

        // Failed attempts have no session yet, so the session id is null.
        // Attempts with an unknown email are not logged (there is no user to attach them to).
        private Task LogFailureAsync(ApplicationUser user, string action, string details)
            => _auditService.LogAsync(null, user.Id, GetDisplayName(user), action, details);

        public async Task<ApiResponse<object>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ApiResponse.Fail("Invalid email or password.", 401);

            if (await _userManager.IsLockedOutAsync(user))
            {
                await LogFailureAsync(user, "LoginBlocked", "tried to log in but the account is disabled");
                return ApiResponse.Fail("This account has been disabled.", 401);
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await LogFailureAsync(user, "LoginFailed", "failed to log in: wrong password");
                return ApiResponse.Fail("Invalid email or password.", 401);
            }

            var otp = _otpService.Generate(user.Id);

            var sent = await _emailService.SendOtpEmailAsync(user.Email!, otp);
            if (!sent)
            {
                _otpService.Remove(user.Id);
                await LogFailureAsync(user, "OtpSendFailed", "entered the right password but the verification code could not be sent");
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

            if (await _userManager.IsLockedOutAsync(user))
            {
                await LogFailureAsync(user, "LoginBlocked", "tried to finish logging in but the account is disabled");
                return ApiResponse.Fail<LoginResponseDto>("This account has been disabled.", 401);
            }

            switch (_otpService.Verify(user.Id, dto.Otp))
            {
                case OtpResult.Expired:
                    await LogFailureAsync(user, "OtpFailed", "used a verification code that had expired or was not active");
                    return ApiResponse.Fail<LoginResponseDto>(
                        "No active code, or it has expired. Please log in again to request a new one.", 400);

                case OtpResult.TooManyAttempts:
                    await LogFailureAsync(user, "OtpLockedOut", "entered too many wrong verification codes; the code was cancelled");
                    return ApiResponse.Fail<LoginResponseDto>(
                        "Too many wrong codes. Please log in again to request a new one.", 429);

                case OtpResult.Invalid:
                    await LogFailureAsync(user, "OtpFailed", "entered a wrong verification code");
                    return ApiResponse.Fail<LoginResponseDto>("Invalid email or code.", 401);
            }

            // ---- the code was correct ----
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var displayName = GetDisplayName(user);

            var sessionId = await _auditService.StartSessionAsync(user.Id, displayName);
            await _auditService.LogAsync(sessionId, user.Id, displayName, "Login", "logged in");

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
                    sessionId, _currentUser.UserId, _currentUser.DisplayName, "Logout", "logged out");
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