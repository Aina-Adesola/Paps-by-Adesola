namespace PAPPSADMIN.Services
{
    // "Who is making this request?" - read from the JWT claims.
    public interface ICurrentUserService
    {
        int UserId { get; }
        string DisplayName { get; }
        int? SessionId { get; }
    }
}
