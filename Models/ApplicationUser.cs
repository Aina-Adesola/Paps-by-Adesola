using Microsoft.AspNetCore.Identity;

namespace TemsGroupProject.Models
{
    // Using IdentityUser<int> instead of the default IdentityUser (which uses string/GUID)
    // gives us an int Id that auto-increments starting at 1.
    public class ApplicationUser : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        public string? OtpCode { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
    }
}