using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Mappers
{
    public static class RequestMapping
    {
        public static FeeRequestResponseDto ToDto(this FeeRequest r) => new()
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
