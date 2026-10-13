using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Services
{
    public interface IAuditService
    {
        Task<int> StartSessionAsync(int userId, string userName);
        Task EndSessionAsync(int sessionId);
        Task LogAsync(int? sessionId, int userId, string userName, string action, string? details = null);

        // Logs an action as "whoever is making the current request".
        Task LogCurrentUserAsync(string action, string? details = null);

        Task<ApiResponse<List<SessionAuditDto>>> GetSessionsAsync(DateTime? fromDate, DateTime? toDate);

        // Flat, readable list of every logged action.
        Task<ApiResponse<List<AuditLogDto>>> GetLogsAsync(DateTime? fromDate, DateTime? toDate);
    }
}