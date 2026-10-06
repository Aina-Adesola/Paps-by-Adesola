namespace TemsGroupProject.DTOs
{
    public class AuditActionDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SessionAuditDto
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public double DurationMinutes { get; set; }
        public bool StillActive { get; set; }
        public List<AuditActionDto> Actions { get; set; } = new();
    }
}