using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.DTOs;

namespace PAPPSADMIN.Controllers
{
    // Every controller inherits from this instead of ControllerBase.
    // FromResponse turns the service's ApiResponse into an HTTP response:
    // the status code comes from response.StatusCode, the body is the envelope.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult FromResponse<T>(ApiResponse<T> response)
            => StatusCode(response.StatusCode, response);
    }
}
