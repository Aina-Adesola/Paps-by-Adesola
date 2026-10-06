using Microsoft.EntityFrameworkCore;
using TemsGroupProject.Data;
using TemsGroupProject.Models;

namespace TemsGroupProject.Services
{
    // Central place every controller calls into to record "this happened."
    // Keeping it in one service means the logging logic lives in one spot
    // instead of being copy-pasted into every controller.
    public class AuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
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
    }
}