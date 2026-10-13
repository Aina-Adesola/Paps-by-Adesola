namespace PAPPSADMIN.DTOs
{
    public class PapsRateResponseDto
    {
        public int Id { get; set; }
        public int FeeRequestId { get; set; }

        public double WemaPercentage { get; set; }
        public double WemaMinimumFee { get; set; }
        public double WemaMaximumFee { get; set; }

        public double PapsPercentage { get; set; }
        public double PapsMinimumFee { get; set; }
        public double PapsMaximumFee { get; set; }

        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
    }
}