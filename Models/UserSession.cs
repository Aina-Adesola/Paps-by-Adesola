namespace TemsGroupProject.Models
{
    // One row per login. LogoutTime stays null until the user calls /api/auth/logout,
    // or (if they never do) is treated as "still active" by the Audit view.
    public class UserSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }
    }
}