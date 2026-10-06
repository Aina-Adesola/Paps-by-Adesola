namespace TemsGroupProject.DTOs
{
    // Every field is optional — SGC can change just the one thing they need to
    // (e.g. only the role) without having to resend everything else.
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? StaffId { get; set; }
        public string? Role { get; set; } // must be one of Roles.AssignableRoles
    }
}