using Microsoft.AspNetCore.Identity;

namespace PAPPSADMIN.Models
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

        // OtpCode and OtpExpiresAt were REMOVED: one-time codes now live in a
        // short-lived memory cache (see Services/OtpService.cs), not in the database.
    }
}