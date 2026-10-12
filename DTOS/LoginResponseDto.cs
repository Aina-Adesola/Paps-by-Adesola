namespace PAPPSADMIN.DTOs
{
    // What verify-otp puts inside the "data" field of the envelope.
    // Previously this was an anonymous object { token, user }; a named class
    // gives Swagger and the front-end a clear, typed shape.
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserResponseDto User { get; set; } = new();
    }
}