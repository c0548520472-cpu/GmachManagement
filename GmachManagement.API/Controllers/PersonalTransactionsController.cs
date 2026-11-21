using System.Security.Claims;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
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

    [HttpGet("{transactionId:int}")]
    public async Task<IActionResult> GetById([FromRoute] int transactionId, CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var transaction = await _service.GetByIdAsync(id.Value, transactionId, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreatePersonalTransactionDto request, CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        try
        {
            var result = await _service.AddAsync(id.Value, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { transactionId = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{transactionId:int}/completions/current")]
    public async Task<IActionResult> Complete([FromRoute] int transactionId, CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        return await _service.CompleteAsync(id.Value, transactionId, cancellationToken) ? NoContent() : NotFound();
    }
}
