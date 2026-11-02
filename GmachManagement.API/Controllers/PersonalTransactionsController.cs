using System.Security.Claims;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
using GmachManagement.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController]
[Authorize]
[Route("api/personal-transactions")]
public class PersonalTransactionsController : ControllerBase
{
    private readonly IPersonalTransactionService _service;

    public PersonalTransactionsController(IPersonalTransactionService service) => _service = service;

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0 ? id : null;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        return id is null ? Unauthorized() : Ok(await _service.GetMineAsync(id.Value, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreatePersonalTransactionDto request, CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        if (!Enum.IsDefined(typeof(TransactionType), request.Type) ||
            string.IsNullOrWhiteSpace(request.OtherPartyName) ||
            string.IsNullOrWhiteSpace(request.ItemDescription) ||
            request.TransactionDate == default || request.TransactionDate > DateTime.UtcNow)
            return BadRequest("Invalid personal transaction details.");
        var result = await _service.AddAsync(id.Value, request, cancellationToken);
        return StatusCode(201, result);
    }

    [HttpPost("{transactionId:int}/complete")]
    public async Task<IActionResult> Complete(int transactionId, CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        return await _service.CompleteAsync(id.Value, transactionId, cancellationToken) ? NoContent() : NotFound();
    }
}
