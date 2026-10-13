using Microsoft.EntityFrameworkCore;
using PAPPSADMIN.Data;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    // Read-only on purpose: the rate is replaced by RequestService when a request is approved.
    public class PapsRateService : IPapsRateService
    {
        private readonly ApplicationDbContext _context;

        public PapsRateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PapsRateResponseDto>> GetCurrentAsync()
        {
            var rate = await _context.PapsRates.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == PapsRate.CurrentRateId);

            if (rate is null)
                return ApiResponse.Fail<PapsRateResponseDto>("No rate has been approved yet.", 404);

            return ApiResponse.Ok(new PapsRateResponseDto
            {
                Id = rate.Id,
                FeeRequestId = rate.FeeRequestId,
                WemaPercentage = rate.WemaPercentage,
                WemaMinimumFee = rate.WemaMinimumFee,
                WemaMaximumFee = rate.WemaMaximumFee,
                PapsPercentage = rate.PapsPercentage,
                PapsMinimumFee = rate.PapsMinimumFee,
                PapsMaximumFee = rate.PapsMaximumFee,
                DateCreated = rate.DateCreated,
                CreatedBy = rate.CreatedBy,
                ApprovedBy = rate.ApprovedBy
            }, "Current Paps rate retrieved successfully.");
        }
    }
}