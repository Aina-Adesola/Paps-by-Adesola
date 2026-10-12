using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Services
{
    public interface IRequestService
    {
        Task<ApiResponse<FeeRequestResponseDto>> CreateRequestAsync(CreateFeeRequestDto dto);
        Task<ApiResponse<List<FeeRequestResponseDto>>> GetMineAsync(string? status, DateTime? fromDate, DateTime? toDate);
        Task<ApiResponse<List<FeeRequestResponseDto>>> GetPendingAsync();
        Task<ApiResponse<List<FeeRequestResponseDto>>> GetAllAsync(string? status, DateTime? fromDate, DateTime? toDate);
        Task<ApiResponse<FeeRequestResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<FeeRequestResponseDto>> ApproveAsync(int id);
        Task<ApiResponse<FeeRequestResponseDto>> DenyAsync(int id);
        Task<ApiResponse<List<FeeRequestResponseDto>>> GetAuditHistoryAsync(DateTime? fromDate, DateTime? toDate);
    }
}
