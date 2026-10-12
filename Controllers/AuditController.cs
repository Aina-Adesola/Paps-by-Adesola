using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.Models;
using PAPPSADMIN.Services;

namespace PAPPSADMIN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Audit)]
    public class AuditController : ApiControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        // GET api/audit/sessions?fromDate=...&toDate=...
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => FromResponse(await _auditService.GetSessionsAsync(fromDate, toDate));
    }
}
