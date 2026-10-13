using Microsoft.EntityFrameworkCore;
using PAPPSADMIN.Data;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AuditService(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<int> StartSessionAsync(int userId, string userName)
        {
            var session = new UserSession
            {
                UserId = userId,
                UserName = userName,
                LoginTime = DateTime.UtcNow
            };
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
            return session.Id;
        }

        public async Task EndSessionAsync(int sessionId)
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session is not null && session.LogoutTime is null)
            {
                session.LogoutTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogAsync(int? sessionId, int userId, string userName, string action, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public Task LogCurrentUserAsync(string action, string? details = null)
            => LogAsync(_currentUser.SessionId, _currentUser.UserId, _currentUser.DisplayName, action, details);

        // Grouped by login session (existing view).
        public async Task<ApiResponse<List<SessionAuditDto>>> GetSessionsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var sessionQuery = _context.UserSessions.AsQueryable();
            if (fromDate.HasValue) sessionQuery = sessionQuery.Where(s => s.LoginTime >= fromDate.Value);
            if (toDate.HasValue) sessionQuery = sessionQuery.Where(s => s.LoginTime <= toDate.Value);

            var sessions = await sessionQuery.OrderByDescending(s => s.LoginTime).ToListAsync();
            var sessionIds = sessions.Select(s => s.Id).ToList();

            var actions = await _context.AuditLogs
                .Where(a => a.SessionId != null && sessionIds.Contains(a.SessionId.Value))
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            var actionsBySession = actions.ToLookup(a => a.SessionId!.Value);

            var result = sessions.Select(s => new SessionAuditDto
            {
                SessionId = s.Id,
                UserId = s.UserId,
                UserName = s.UserName,
                LoginTime = s.LoginTime,
                LogoutTime = s.LogoutTime,
                DurationMinutes = Math.Round(((s.LogoutTime ?? DateTime.UtcNow) - s.LoginTime).TotalMinutes, 1),
                StillActive = s.LogoutTime is null,
                Actions = actionsBySession[s.Id]
                    .Select(a => new AuditActionDto { Action = a.Action, Details = a.Details, Timestamp = a.Timestamp })
                    .ToList()
            }).ToList();

            return ApiResponse.Ok(result, "Audit sessions retrieved successfully.");
        }

        // NEW: one flat list of everything that happened, newest first, as readable sentences.
        public async Task<ApiResponse<List<AuditLogDto>>> GetLogsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.AuditLogs.AsQueryable();
            if (fromDate.HasValue) query = query.Where(a => a.Timestamp >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(a => a.Timestamp <= toDate.Value);

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var result = logs.Select(a => new AuditLogDto
            {
                Id = a.Id,
                SessionId = a.SessionId,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                Details = a.Details,
                // e.g. "Paul changed Peter Obi's role from Audit to Requester"
                Description = string.IsNullOrWhiteSpace(a.Details)
                    ? $"{a.UserName}: {a.Action}"
                    : $"{a.UserName} {a.Details}",
                Timestamp = a.Timestamp
            }).ToList();

            return ApiResponse.Ok(result, "Audit logs retrieved successfully.");
        }
    }
}