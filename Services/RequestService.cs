using Microsoft.EntityFrameworkCore;
using PAPPSADMIN.Data;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Mappers;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;

        public RequestService(
            ApplicationDbContext context,
            IAuditService auditService,
            ICurrentUserService currentUser)
        {
            _context = context;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<FeeRequestResponseDto>> CreateRequestAsync(CreateFeeRequestDto dto)
        {
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
                CreatedBy = _currentUser.DisplayName,
                CreatedByUserId = _currentUser.UserId
            };

            _context.FeeRequests.Add(request);
            await _context.SaveChangesAsync();

            await _auditService.LogCurrentUserAsync("CreatedRequest", $"Created request {request.Id}");

            return ApiResponse.Ok(request.ToDto(), "Request created and is waiting for approval.", 201);
        }

        public async Task<ApiResponse<List<FeeRequestResponseDto>>> GetMineAsync(
            string? status, DateTime? fromDate, DateTime? toDate)
        {
            var currentUserId = _currentUser.UserId;

            var query = _context.FeeRequests.Where(r => r.CreatedByUserId == currentUserId);
            query = ApplyStatusFilter(query, status);
            query = ApplyDateFilter(query, fromDate, toDate);

            var results = await query.OrderByDescending(r => r.DateCreated).ToListAsync();
            return ApiResponse.Ok(results.Select(r => r.ToDto()).ToList(), "Your requests were retrieved successfully.");
        }

        public async Task<ApiResponse<List<FeeRequestResponseDto>>> GetPendingAsync()
        {
            var pending = await _context.FeeRequests
                .Where(r => r.ApprovalStatus == RequestStatus.Pending)
                .OrderBy(r => r.DateCreated)
                .ToListAsync();

            return ApiResponse.Ok(pending.Select(r => r.ToDto()).ToList(), "Pending requests retrieved successfully.");
        }

        public async Task<ApiResponse<List<FeeRequestResponseDto>>> GetAllAsync(
            string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FeeRequests.AsQueryable();
            query = ApplyStatusFilter(query, status);
            query = ApplyDateFilter(query, fromDate, toDate);

            var results = await query.OrderByDescending(r => r.DateCreated).ToListAsync();
            return ApiResponse.Ok(results.Select(r => r.ToDto()).ToList(), "Requests retrieved successfully.");
        }

        public async Task<ApiResponse<FeeRequestResponseDto>> GetByIdAsync(int id)
        {
            var request = await _context.FeeRequests.FindAsync(id);
            if (request is null)
                return ApiResponse.Fail<FeeRequestResponseDto>("Request not found.", 404);

            return ApiResponse.Ok(request.ToDto(), "Request retrieved successfully.");
        }

        public Task<ApiResponse<FeeRequestResponseDto>> ApproveAsync(int id)
            => DecideAsync(id, RequestStatus.Approved);

        public Task<ApiResponse<FeeRequestResponseDto>> DenyAsync(int id)
            => DecideAsync(id, RequestStatus.Denied);

        public async Task<ApiResponse<List<FeeRequestResponseDto>>> GetAuditHistoryAsync(
            DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FeeRequests
                .Where(r => r.ApprovalStatus == RequestStatus.Approved || r.ApprovalStatus == RequestStatus.Denied);
            query = ApplyDateFilter(query, fromDate, toDate);

            var decided = await query.OrderByDescending(r => r.DateApproved).ToListAsync();
            return ApiResponse.Ok(decided.Select(r => r.ToDto()).ToList(), "Decided requests retrieved successfully.");
        }

        private async Task<ApiResponse<FeeRequestResponseDto>> DecideAsync(int id, int newStatus)
        {
            var request = await _context.FeeRequests.FindAsync(id);
            if (request is null)
                return ApiResponse.Fail<FeeRequestResponseDto>("Request not found.", 404);

            if (request.ApprovalStatus != RequestStatus.Pending)
                return ApiResponse.Fail<FeeRequestResponseDto>("This request has already been decided.", 400);

            request.ApprovalStatus = newStatus;
            request.DateApproved = DateTime.UtcNow;
            // Recorded even when denied - "ApprovedBy" always means "who made the decision".
            request.ApprovedBy = _currentUser.DisplayName;
            request.ApprovedByUserId = _currentUser.UserId;

            await _context.SaveChangesAsync();

            var approved = newStatus == RequestStatus.Approved;
            await _auditService.LogCurrentUserAsync(approved ? "ApprovedRequest" : "DeniedRequest", $"Request {id}");

            return ApiResponse.Ok(request.ToDto(), $"Request {id} has been {(approved ? "approved" : "denied")}.");
        }

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

        private static IQueryable<FeeRequest> ApplyDateFilter(IQueryable<FeeRequest> query, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue) query = query.Where(r => r.DateCreated >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(r => r.DateCreated <= toDate.Value);
            return query;
        }
    }
}
