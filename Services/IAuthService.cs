using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> LoginAsync(LoginDto dto);
        Task<ApiResponse<LoginResponseDto>> VerifyOtpAsync(VerifyOtpDto dto);
        Task<ApiResponse<object>> LogoutAsync();
    }
}
