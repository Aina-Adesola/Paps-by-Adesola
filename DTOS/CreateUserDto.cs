using System.ComponentModel.DataAnnotations;

namespace TemsGroupProject.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string StaffId { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // Must be one of: Audit, Requester, Approver
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}