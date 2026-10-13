namespace PAPPSADMIN.DTOs
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

    // One line of the full audit trail.
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? SessionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }

        // The whole sentence, e.g. "Paul changed Peter Obi's role from Audit to Requester".
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}