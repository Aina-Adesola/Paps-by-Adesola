using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemsGroupProject.Data;
using TemsGroupProject.DTOs;
using TemsGroupProject.Models;
using TemsGroupProject.Services;

namespace TemsGroupProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // every endpoint here requires a logged-in user; specific roles enforced per-action
    public class RequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService; // <-- ADDED

        public RequestsController(ApplicationDbContext context, AuditService auditService) // <-- CHANGED
        {
            _context = context;
            _auditService = auditService; // <-- ADDED
        }

        // POST api/requests — Requester creates a new fee request
        [Authorize(Roles = Roles.Requester)]
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateFeeRequestDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var request = new FeeRequest
            {
                WemaPercentage = dto.WemaPercentage,
                WemaMinimumFee = dto.WemaMinimumFee,
                WemaMaximumFee = dto.WemaMaximumFee,
                PapsPercentage = dto.PapsPercentage,
                PapsMinimumFee = dto.PapsMinimumFee,
                PapsMaximumFee = dto.PapsMaximumFee,
                DateCreated = DateTime.UtcNow,
                ApprovalStatus = RequestStatus.Pending,
                CreatedBy = GetCurrentUserDisplayName(),
                CreatedByUserId = currentUserId // <-- ADDED
            };

            _context.FeeRequests.Add(request);
            await _context.SaveChangesAsync();

            await LogAuditAsync("CreatedRequest", $"Created request {request.Id}"); // <-- ADDED

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, ToResponseDto(request));
        }

        // <-- ADDED: GET api/requests/mine — Requester sees only their own requests.
        // status: "pending" | "approved" | "denied" (omit for all).
        // fromDate / toDate filter by DateCreated, e.g. ?fromDate=2026-01-01
        [Authorize(Roles = Roles.Requester)]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine([FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var currentUserId = GetCurrentUserId();

            var query = _context.FeeRequests.Where(r => r.CreatedByUserId == currentUserId);
            query = ApplyStatusFilter(query, status);
            query = ApplyDateFilter(query, fromDate, toDate);

            var results = await query.OrderByDescending(r => r.DateCreated).ToListAsync();
            return Ok(results.Select(ToResponseDto));
        }

        // GET api/requests/pending — Approver sees everything awaiting a decision
        [Authorize(Roles = Roles.Approver)]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _context.FeeRequests
                .Where(r => r.ApprovalStatus == RequestStatus.Pending)
                .OrderBy(r => r.DateCreated)
                .ToListAsync();

            return Ok(pending.Select(ToResponseDto));
        }

        // <-- ADDED: GET api/requests — broader Approver listing with the same
        // status/date filters as "mine", but across every request.
        [Authorize(Roles = Roles.Approver)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var query = _context.FeeRequests.AsQueryable();
            query = ApplyStatusFilter(query, status);
            query = ApplyDateFilter(query, fromDate, toDate);

            var results = await query.OrderByDescending(r => r.DateCreated).ToListAsync();
            return Ok(results.Select(ToResponseDto));
        }

        // GET api/requests/{id} — Approver opens one request to review it in detail;
        // Audit can also look up an individual record.
        [Authorize(Roles = $"{Roles.Approver},{Roles.Audit}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.FeeRequests.FindAsync(id);
            if (request is null) return NotFound();

            return Ok(ToResponseDto(request));
        }

        // POST api/requests/{id}/approve
        [Authorize(Roles = Roles.Approver)]
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            return await Decide(id, RequestStatus.Approved);
        }

        // POST api/requests/{id}/deny
        [Authorize(Roles = Roles.Approver)]
        [HttpPost("{id:int}/deny")]
        public async Task<IActionResult> Deny(int id)
        {
            return await Decide(id, RequestStatus.Denied);
        }

        // GET api/requests/audit — Audit sees every decided request, with the same
        // date filters as everywhere else.
        [Authorize(Roles = Roles.Audit)]
        [HttpGet("audit")]
        public async Task<IActionResult> GetAuditHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) // <-- CHANGED: filters added
        {
            var query = _context.FeeRequests
                .Where(r => r.ApprovalStatus == RequestStatus.Approved || r.ApprovalStatus == RequestStatus.Denied);
            query = ApplyDateFilter(query, fromDate, toDate);

            var decided = await query.OrderByDescending(r => r.DateApproved).ToListAsync();
            return Ok(decided.Select(ToResponseDto));
        }

        private async Task<IActionResult> Decide(int id, int newStatus)
        {
            var request = await _context.FeeRequests.FindAsync(id);
            if (request is null) return NotFound();

            if (request.ApprovalStatus != RequestStatus.Pending)
                return BadRequest(new { message = "This request has already been decided." });

            request.ApprovalStatus = newStatus;
            request.DateApproved = DateTime.UtcNow;
            // Recorded even when denied — the column is "ApprovedBy" but it always
            // means "who made the decision," per the spec.
            request.ApprovedBy = GetCurrentUserDisplayName();
            request.ApprovedByUserId = GetCurrentUserId(); // <-- ADDED

            await _context.SaveChangesAsync();

            // <-- ADDED
            var actionName = newStatus == RequestStatus.Approved ? "ApprovedRequest" : "DeniedRequest";
            await LogAuditAsync(actionName, $"Request {id}");

            return Ok(ToResponseDto(request));
        }

        // <-- ADDED
        private static IQueryable<FeeRequest> ApplyStatusFilter(IQueryable<FeeRequest> query, string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return query;

            return status.Trim().ToLowerInvariant() switch
            {
                "pending" => query.Where(r => r.ApprovalStatus == RequestStatus.Pending),
                "approved" => query.Where(r => r.ApprovalStatus == RequestStatus.Approved),
                "denied" => query.Where(r => r.ApprovalStatus == RequestStatus.Denied),
                _ => query
            };
        }

        // <-- ADDED
        private static IQueryable<FeeRequest> ApplyDateFilter(IQueryable<FeeRequest> query, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue) query = query.Where(r => r.DateCreated >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(r => r.DateCreated <= toDate.Value);
            return query;
        }

        private int GetCurrentUserId() // <-- ADDED
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        private string GetCurrentUserDisplayName()
        {
            var firstName = User.FindFirstValue(ClaimTypes.GivenName);
            var lastName = User.FindFirstValue(ClaimTypes.Surname);
            var email = User.FindFirstValue(ClaimTypes.Email);

            var fullName = $"{firstName} {lastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : (email ?? "Unknown");
        }

        // <-- ADDED: mirrors the helper in UserManagementController
        private async Task LogAuditAsync(string action, string details)
        {
            var sessionIdClaim = User.FindFirstValue("sid");
            var sessionId = int.TryParse(sessionIdClaim, out var sid) ? sid : (int?)null;

            await _auditService.LogAsync(sessionId, GetCurrentUserId(), GetCurrentUserDisplayName(), action, details);
        }

        private static FeeRequestResponseDto ToResponseDto(FeeRequest r) => new()
        {
            Id = r.Id,
            WemaPercentage = r.WemaPercentage,
            WemaMinimumFee = r.WemaMinimumFee,
            WemaMaximumFee = r.WemaMaximumFee,
            PapsPercentage = r.PapsPercentage,
            PapsMinimumFee = r.PapsMinimumFee,
            PapsMaximumFee = r.PapsMaximumFee,
            DateCreated = r.DateCreated,
            DateApproved = r.DateApproved,
            ApprovalStatus = r.ApprovalStatus,
            CreatedBy = r.CreatedBy,
            ApprovedBy = r.ApprovedBy
        };
    }
}