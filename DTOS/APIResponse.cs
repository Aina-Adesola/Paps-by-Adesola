using System.Text.Json.Serialization;

namespace PAPPSADMIN.DTOs
{
    // The "envelope" every endpoint returns. T is a blank that gets filled in with
    // whatever kind of data that particular endpoint sends back.
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        // NEW: which HTTP status the controller should send (200, 201, 400, 404...).
        // [JsonIgnore] keeps it out of the JSON body; it only travels between the
        // service and the controller.
        [JsonIgnore]
        public int StatusCode { get; set; } = 200;
    }

    public static class ApiResponse
    {
        // Success WITH data:  ApiResponse.Ok(user, "User retrieved.")
        public static ApiResponse<T> Ok<T>(T data, string message, int statusCode = 200) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };

        // Success with NO data, just a message:  ApiResponse.Ok("Logged out.")
        public static ApiResponse<object> Ok(string message) => new()
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };

        // Failure where the method must return ApiResponse<SomeType>:
        //   return ApiResponse.Fail<UserResponseDto>("User not found.", 404);
        public static ApiResponse<T> Fail<T>(string message, int statusCode = 400, IEnumerable<string>? errors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList(),
            StatusCode = statusCode
        };

        // Failure where there is no data type:  ApiResponse.Fail("Invalid email or password.", 401)
        public static ApiResponse<object> Fail(string message, int statusCode = 400, IEnumerable<string>? errors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList(),
            StatusCode = statusCode
        };
    }
}
