using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Models;
using PAPPSADMIN.Services;

namespace PAPPSADMIN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // every endpoint needs a logged-in user; roles enforced per action
    public class RequestsController : ApiControllerBase
    {
        private readonly IRequestService _requestService;

        public RequestsController(IRequestService requestService)
        {
            _requestService = requestService;
        }

        // POST api/requests - Requester creates a new fee request
        [Authorize(Roles = Roles.Requester)]
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateFeeRequestDto dto)
            => FromResponse(await _requestService.CreateRequestAsync(dto));

        // GET api/requests/mine - Requester sees only their own requests
        [Authorize(Roles = Roles.Requester)]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine([FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => FromResponse(await _requestService.GetMineAsync(status, fromDate, toDate));

        // GET api/requests/pending - Approver sees everything awaiting a decision
        [Authorize(Roles = Roles.Approver)]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
            => FromResponse(await _requestService.GetPendingAsync());

        // GET api/requests - Approver sees every request, with filters
        [Authorize(Roles = Roles.Approver)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => FromResponse(await _requestService.GetAllAsync(status, fromDate, toDate));

        // GET api/requests/{id} - Approver (or Audit) opens one request
        [Authorize(Roles = $"{Roles.Approver},{Roles.Audit}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
            => FromResponse(await _requestService.GetByIdAsync(id));

        [Authorize(Roles = Roles.Approver)]
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
            => FromResponse(await _requestService.ApproveAsync(id));

        [Authorize(Roles = Roles.Approver)]
        [HttpPost("{id:int}/deny")]
        public async Task<IActionResult> Deny(int id)
            => FromResponse(await _requestService.DenyAsync(id));

        // GET api/requests/audit - Audit sees every decided request
        [Authorize(Roles = Roles.Audit)]
        [HttpGet("audit")]
        public async Task<IActionResult> GetAuditHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => FromResponse(await _requestService.GetAuditHistoryAsync(fromDate, toDate));
    }
}
