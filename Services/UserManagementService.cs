using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Mappers;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto)
        {
            // Expected problems: check first, answer politely. No exceptions needed.
            if (!Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                return ApiResponse.Fail<UserResponseDto>(
                    $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}", 400);

            if (await _userManager.FindByEmailAsync(dto.Email) is not null)
                return ApiResponse.Fail<UserResponseDto>("A user with this email already exists.", 409);

            if (await _userManager.Users.AnyAsync(u => u.StaffId == dto.StaffId))
                return ApiResponse.Fail<UserResponseDto>("A user with this Staff ID already exists.", 409);

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                StaffId = dto.StaffId,
                DateCreated = DateTime.UtcNow
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, dto.Password);
            }
            catch (DbUpdateException ex)
            {
                // Unexpected problem we can still explain (e.g. two admins creating the
                // same Staff ID at the same moment, so the unique index rejects one).
                _logger.LogWarning(ex, "Database rejected new user {Email}", dto.Email);
                return ApiResponse.Fail<UserResponseDto>(
                    "The user could not be saved. The email or Staff ID may already be in use.", 409);
            }

            if (!result.Succeeded)
                return ApiResponse.Fail<UserResponseDto>(
                    "Could not create the user.", 400, result.Errors.Select(e => e.Description));

            var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
            {
                // Don't leave a half-created user behind (an account that has no role).
                await _userManager.DeleteAsync(user);
                return ApiResponse.Fail<UserResponseDto>(
                    "The role could not be assigned, so the new user was not created.", 500,
                    roleResult.Errors.Select(e => e.Description));
            }

            await _auditService.LogCurrentUserAsync(
                "CreatedUser", $"Created user {user.Id} ({user.Email}) with role {dto.Role}");

            return ApiResponse.Ok(user.ToDto(new[] { dto.Role }), "User created successfully.", 201);
        }

        public async Task<ApiResponse<List<UserResponseDto>>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.OrderBy(u => u.Id).ToListAsync();

            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(user.ToDto(roles));
            }

            return ApiResponse.Ok(result, "Users retrieved successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> GetUserByStaffIdAsync(string staffId)
        {
            if (string.IsNullOrWhiteSpace(staffId))
                return ApiResponse.Fail<UserResponseDto>("Staff ID is required.", 400);

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StaffId == staffId.Trim());
            if (user is null)
                return ApiResponse.Fail<UserResponseDto>("User not found.", 404);

            var roles = await _userManager.GetRolesAsync(user);
            return ApiResponse.Ok(user.ToDto(roles), "User retrieved successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> EditUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail<UserResponseDto>("User not found.", 404);

            // Validate the role BEFORE saving anything, so a bad role can't leave
            // the profile half-updated.
            if (!string.IsNullOrWhiteSpace(dto.Role) &&
                !Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                return ApiResponse.Fail<UserResponseDto>(
                    $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}", 400);

            if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.StaffId)) user.StaffId = dto.StaffId;

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _userManager.FindByEmailAsync(dto.Email);
                if (emailTaken is not null && emailTaken.Id != user.Id)
                    return ApiResponse.Fail<UserResponseDto>("Another user already has this email.", 409);

                user.Email = dto.Email;
                user.UserName = dto.Email; // login uses the email as the username
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return ApiResponse.Fail<UserResponseDto>(
                    "Could not update the user.", 400, updateResult.Errors.Select(e => e.Description));

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            await _auditService.LogCurrentUserAsync("EditedUser", $"Edited user {user.Id} ({user.Email})");

            var roles = await _userManager.GetRolesAsync(user);
            return ApiResponse.Ok(user.ToDto(roles), "User updated successfully.");
        }

        public async Task<ApiResponse<object>> DisableUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail("User not found.", 404);

            user.LockoutEnabled = true;
            var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!lockoutResult.Succeeded)
                return ApiResponse.Fail(
                    "Could not disable the user.", 400, lockoutResult.Errors.Select(e => e.Description));

            await _auditService.LogCurrentUserAsync("DisabledUser", $"Disabled user {id}");
            return ApiResponse.Ok($"User {id} has been disabled.");
        }

        public async Task<ApiResponse<object>> EnableUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail("User not found.", 404);

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
                return ApiResponse.Fail(
                    "Could not re-enable the user.", 400, result.Errors.Select(e => e.Description));

            await _auditService.LogCurrentUserAsync("EnabledUser", $"Enabled user {id}");
            return ApiResponse.Ok($"User {id} has been re-enabled.");
        }
    }
}
