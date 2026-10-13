using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Services
{
    public interface IPapsRateService
    {
        // The one rate currently in force.
        Task<ApiResponse<PapsRateResponseDto>> GetCurrentAsync();
    }
}