using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.Services;

namespace PAPPSADMIN.Controllers
{
    // Route is api/PapssRate. There is deliberately no POST/PUT/DELETE here:
    // the rate is replaced automatically when an approver approves a request.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // any logged-in user; tighten with Roles = ... if needed
    public class PapssRateController : ApiControllerBase
    {
        private readonly IPapsRateService _rateService;

        public PapssRateController(IPapsRateService rateService)
        {
            _rateService = rateService;
        }

        // GET api/papssrate - the one rate currently in force
        [HttpGet]
        public async Task<IActionResult> GetCurrent()
            => FromResponse(await _rateService.GetCurrentAsync());
    }
}