using System.Security.Claims;

namespace PAPPSADMIN.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUserService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public int UserId =>
            int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public int? SessionId =>
            int.TryParse(Principal?.FindFirstValue("sid"), out var sid) ? sid : null;

        public string DisplayName
        {
            get
            {
                var first = Principal?.FindFirstValue(ClaimTypes.GivenName);
                var last = Principal?.FindFirstValue(ClaimTypes.Surname);
                var email = Principal?.FindFirstValue(ClaimTypes.Email);

                var fullName = $"{first} {last}".Trim();
                return !string.IsNullOrWhiteSpace(fullName) ? fullName : (email ?? "Unknown");
            }
        }
    }
}
