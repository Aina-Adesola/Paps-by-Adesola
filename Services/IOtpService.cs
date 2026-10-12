namespace PAPPSADMIN.Services
{
    public enum OtpResult
    {
        Valid,
        Invalid,          // wrong code, attempts remain
        Expired,          // no active code (expired, never issued, or already used)
        TooManyAttempts   // too many wrong guesses; the code was destroyed
    }

    public interface IOtpService
    {
        // Creates a new code for the user, keeps it in memory for 5 minutes and
        // returns the plain code so it can be emailed.
        string Generate(int userId);

        OtpResult Verify(int userId, string code);

        // Throws the code away (used when the email could not be sent).
        void Remove(int userId);
    }
}