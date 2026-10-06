namespace TemsGroupProject.DTOs
{
    // Only the fields a Requester actually fills in — everything else
    // (dates, status, who created/approved it) is set by the system.
    public class CreateFeeRequestDto
    {
        public double WemaPercentage { get; set; }
        public double WemaMinimumFee { get; set; }
        public double WemaMaximumFee { get; set; }

        public double PapsPercentage { get; set; }
        public double PapsMinimumFee { get; set; }
        public double PapsMaximumFee { get; set; }
    }

    public class FeeRequestResponseDto
    {
        public int Id { get; set; }
        public double WemaPercentage { get; set; }
        public double WemaMinimumFee { get; set; }
        public double WemaMaximumFee { get; set; }
        public double PapsPercentage { get; set; }
        public double PapsMinimumFee { get; set; }
        public double PapsMaximumFee { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateApproved { get; set; }
        public int ApprovalStatus { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
    }
}