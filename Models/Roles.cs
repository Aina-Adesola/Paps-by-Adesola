namespace TemsGroupProject.Models
{
    public static class Roles
    {
        public const string Audit = "Audit";
        public const string Requester = "Requester";
        public const string Approver = "Approver";
        public const string SGC = "SGC";

        // The four roles an SGC is allowed to assign when creating a user.
        // SGC accounts themselves are not created through the API.
        public static readonly string[] AssignableRoles = { Audit, Requester, Approver, SGC };
    }
}