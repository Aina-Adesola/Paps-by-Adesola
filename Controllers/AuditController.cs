using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemsGroupProject.Data;
using TemsGroupProject.DTOs;
using TemsGroupProject.Models;

namespace TemsGroupProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Audit)]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/audit/sessions?fromDate=...&toDate=...
        // Returns every login session, who it belonged to, how long it lasted,
        // and what the person did during it.
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
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

            var result = sessions.Select(s => new SessionAuditDto
            {
                SessionId = s.Id,
                UserId = s.UserId,
                UserName = s.UserName,
                LoginTime = s.LoginTime,
                LogoutTime = s.LogoutTime,
                DurationMinutes = Math.Round(((s.LogoutTime ?? DateTime.UtcNow) - s.LoginTime).TotalMinutes, 1),
                StillActive = s.LogoutTime is null,
                Actions = actions.Where(a => a.SessionId == s.Id)
                    .Select(a => new AuditActionDto { Action = a.Action, Details = a.Details, Timestamp = a.Timestamp })
                    .ToList()
            });

            return Ok(result);
        }
    }
}