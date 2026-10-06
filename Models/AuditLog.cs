namespace TemsGroupProject.Models
{
    // One row per notable action taken in the system (login, logout, create user,
    // disable user, edit user, create request, approve/deny request). The Audit
    // role reads these to answer "who did what, and when."
    public class AuditLog
    {
        public int Id { get; set; }

        // Links this action to the login session it happened during, so the Audit
        // view can group "everything this person did between logging in and out."
        public int? SessionId { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}