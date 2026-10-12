using Microsoft.AspNetCore.Diagnostics;
using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Handlers
{
    // The safety net: any exception nobody caught ends up here.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // 1. Record the full details for developers (stack trace included).
            _logger.LogError(exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            // 2. Tell the client something friendly. Technical details are shown
            //    ONLY while developing, never in production.
            var errors = _env.IsDevelopment()
                ? new[] { $"{exception.GetType().Name}: {exception.Message}" }
                : null;

            var response = ApiResponse.Fail(
                "Something went wrong on our side. Please try again later.", 500, errors);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            // true = "I handled it, nobody else needs to".
            return true;
        }
    }
}
