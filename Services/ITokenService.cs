using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public interface ITokenService
    {
        string GenerateToken(ApplicationUser user, IEnumerable<string> roles, int sessionId);
    }
}
