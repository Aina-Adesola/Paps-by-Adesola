namespace TemsGroupProject.Models
{
    public class FeeRequest
    {
        public int Id { get; set; }

        public double WemaPercentage { get; set; }
        public double WemaMinimumFee { get; set; }
        public double WemaMaximumFee { get; set; }

        public double PapsPercentage { get; set; }
        public double PapsMinimumFee { get; set; }
        public double PapsMaximumFee { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateApproved { get; set; }

        // 1 = Pending, 0 = Denied, 2 = Approved (see RequestStatus)
        public int ApprovalStatus { get; set; } = RequestStatus.Pending;

        public string CreatedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }

        // <-- ADDED: reliable ownership/auditing by ID, since CreatedBy/ApprovedBy
        // are just display-name snapshots and could go stale if SGC edits a user's
        // name later. These two drive "show me only my requests" filtering and are
        // not shown in the API response — CreatedBy/ApprovedBy strings are what's
        // actually displayed, matching the table shape from the spec.
        public int CreatedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
    }
}