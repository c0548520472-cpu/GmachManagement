using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GmachManagement.API.Controllers;

[ApiController, Authorize, Route("api/gmach-communication")]
public sealed class GmachCommunicationController(
    GmachCommunicationService service,
    IHttpClientFactory clients,
    IConfiguration config,
    ILogger<GmachCommunicationController> logger) : ControllerBase
{
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("approvals")]
    public async Task<IActionResult> Approvals(CancellationToken ct)
        => ActorId > 0 ? Ok(await service.GetApprovalsAsync(ActorId, ct)) : Unauthorized();

    [HttpPost("approvals/{id:int}/seen")]
    public async Task<IActionResult> MarkSeen(int id, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        await service.MarkApprovalSeenAsync(ActorId, id, ct);
        return NoContent();
    }

    [Authorize(Roles = "GmachAdmin"), HttpGet("inbox")]
    public async Task<IActionResult> Inbox(CancellationToken ct)
        => ActorId > 0 ? Ok(await service.GetOwnerInboxAsync(ActorId, ct)) : Unauthorized();

    [HttpGet("{gmachId:int}/messages")]
    public async Task<IActionResult> Chat(int gmachId, [FromQuery] int? customerId, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        var messages = await service.GetChatAsync(gmachId, customerId, ActorId, ct);
        if (messages == null) return NotFound();
        return Ok(messages.AsEnumerable().Reverse().Select(message => new
        {
            message.Id, message.Text, message.SentAt, Mine = message.SenderId == ActorId
        }));
    }

    public sealed record NewChatMessage([StringLength(1000)] string? Text, [Range(1, int.MaxValue)] int? ItemId);

    [EnableRateLimiting("gmach-chat-send"), HttpPost("{gmachId:int}/messages")]
    public async Task<IActionResult> Send(int gmachId, [FromQuery] int? customerId, NewChatMessage input, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        var message = input.Text?.Trim() ?? "";
        if ((input.ItemId.HasValue && (customerId.HasValue || message.Length != 0)) ||
            (!input.ItemId.HasValue && message.Length == 0))
            return BadRequest(new { message = "יש לכתוב הודעה או לבחור פריט." });

        var saved = await service.AddMessageAsync(gmachId, customerId, ActorId, message, input.ItemId, ct);
        if (!saved.Saved) return NotFound();
        if (saved.OwnerEmail is { } email)
            await NotifyOwnerAsync(email);
        return NoContent();
    }

    private async Task NotifyOwnerAsync(string ownerEmail)
    {
        var apiKey = config["Email:ResendApiKey"];
        var sender = config["Email:From"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sender))
        {
            logger.LogWarning("Chat email skipped because Email:ResendApiKey or Email:From is missing.");
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            from = sender,
            to = new[] { ownerEmail },
            subject = "הודעה חדשה בצ׳אט הגמ״ח",
            text = "ממתינה לך הודעה חדשה בצ׳אט. לצפייה ולמענה יש להתחבר לניהול הגמ״ח: https://gmachmanagement-client.pages.dev/?page=owner"
        });

        try
        {
            using var response = await clients.CreateClient("GmachChatEmail").SendAsync(request);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Chat email provider returned HTTP {StatusCode}.", (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Chat email delivery failed.");
        }
    }
}
