namespace PAPPSADMIN.Models
{
    // The Paps Rates table holds exactly ONE row: the rate currently in force.
    // When an approver approves a request, that row is REPLACED with the request's values
    // (or created, the very first time). Nobody can add or edit it directly.
    // The history of past rates lives in the audit log, not in this table.
    public class PapsRate
    {
        // The single row always has this Id. Because the Id is a primary key and is never
        // auto-generated (see ApplicationDbContext), the database itself refuses a second row.
        public const int CurrentRateId = 1;

        public int Id { get; set; } = CurrentRateId;

        // The approved request these values came from.
        public int FeeRequestId { get; set; }

        public double WemaPercentage { get; set; }
        public double WemaMinimumFee { get; set; }
        public double WemaMaximumFee { get; set; }

        public double PapsPercentage { get; set; }
        public double PapsMinimumFee { get; set; }
        public double PapsMaximumFee { get; set; }

        // When these values were put in place, i.e. the moment of approval.
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // Who made the request.
        public string CreatedBy { get; set; } = string.Empty;

        // Who approved it.
        public string ApprovedBy { get; set; } = string.Empty;
    }
}