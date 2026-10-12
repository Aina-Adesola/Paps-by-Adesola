using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Mappers
{
    // The "user -> UserResponseDto" code used to be copy-pasted four times.
    // Now it lives here once.
    public static class UserMapping
    {
        public static UserResponseDto ToDto(this ApplicationUser user, IEnumerable<string> roles) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            StaffId = user.StaffId,
            Role = roles.FirstOrDefault() ?? string.Empty,
            DateCreated = user.DateCreated,
            LastLogin = user.LastLogin,
            IsDisabled = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow
        };
    }
}
