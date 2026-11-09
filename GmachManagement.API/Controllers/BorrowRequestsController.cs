using System.Security.Claims;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/borrow-requests")]
public class BorrowRequestsController : ControllerBase
{
    private readonly IBorrowRequestService _service;
    public BorrowRequestsController(IBorrowRequestService service) => _service = service;

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateBorrowRequestDto dto, CancellationToken ct)
    {
        var request = await _service.CreateAsync(dto.ItemCopyId, dto.ApplicantName, dto.Phone, dto.Email, ct);
        if (request == null) return NotFound(new { message = "הפריט אינו זמין לבקשה" });
        return StatusCode(201, new { message = "הפרטים הועברו לבעל הגמ״ח והוא יצור איתך קשר" });
    }

    [HttpGet("pending"), Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
        var requests = await _service.GetPendingForOwnerAsync(ownerId, ct);
        return Ok(requests.Select(ToDto));
    }

    [HttpPost("{id:int}/fulfill"), Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Fulfill(int id, CancellationToken ct)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
        try
        {
            var request = await _service.FulfillAsync(id, ownerId, ct);
            return request == null ? NotFound() : Ok(ToDto(request));
        }
        catch (ConcurrencyConflictException)
        {
            return Conflict(new { message = "הפריט כבר אינו זמין להשאלה" });
        }
    }

    private bool TryGetOwnerId(out int ownerId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out ownerId) && ownerId > 0;

    private static BorrowRequestDto ToDto(BorrowRequest r) => new()
    {
        Id = r.Id,
        ItemCopyId = r.ItemCopyId,
        ItemId = r.ItemCopy.ItemId,
        ItemName = r.ItemCopy.Item.Name,
        ApplicantName = r.ApplicantName,
        Phone = r.Phone,
        Email = r.Email,
        CreatedAt = r.CreatedAt,
        FulfilledAt = r.FulfilledAt
    };
}
