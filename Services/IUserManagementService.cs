using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Services
{
    public interface IUserManagementService
    {
        Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto);
        Task<ApiResponse<List<UserResponseDto>>> GetAllUsersAsync();
        Task<ApiResponse<UserResponseDto>> GetUserByStaffIdAsync(string staffId);
        Task<ApiResponse<UserResponseDto>> EditUserAsync(int id, UpdateUserDto dto);
        Task<ApiResponse<object>> DisableUserAsync(int id);
        Task<ApiResponse<object>> EnableUserAsync(int id);
    }
}
