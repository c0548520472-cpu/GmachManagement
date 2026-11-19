using Microsoft.Extensions.Caching.Memory;
using GmachManagement.Core.Interfaces;
using GmachManagement.API.Services;
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
    ResendEmailService email,
    IMemoryCache cache,
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
    public async Task<IActionResult> Chat(int gmachId, [FromQuery] int? customerId, [FromQuery] bool presence, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        var messages = await service.GetChatAsync(gmachId, customerId, ActorId, ct);
        if (messages == null) return NotFound();
        var result = messages.AsEnumerable().Reverse().Select(message => new
        {
            message.Id, message.Text, message.SentAt, message.AttachmentName, Mine = message.SenderId == ActorId,
            ReadByOwner = message.ReadByOwnerAt.HasValue,
            ReadByRecipient = message.SenderId == (customerId ?? ActorId)
                ? message.ReadByOwnerAt.HasValue : message.ReadByCustomerAt.HasValue
        });
        if (!presence) return Ok(result);
        var counterpartTyping = cache.TryGetValue(TypingKey(gmachId, customerId ?? ActorId, !customerId.HasValue), out _);
        return Ok(new { messages = result, counterpartTyping });
    }

    public sealed record ChatReadReceipt([Range(1, int.MaxValue)] int ThroughId);

    [HttpPost("{gmachId:int}/messages/read")]
    public async Task<IActionResult> MarkChatRead(int gmachId, [FromQuery] int? customerId, ChatReadReceipt input, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        if (gmachId <= 0 || customerId <= 0) return BadRequest();
        if (customerId is int customer)
        {
            if (!User.IsInRole("GmachAdmin")) return Forbid();
            await service.MarkChatReadByOwnerAsync(gmachId, customer, ActorId, input.ThroughId, ct);
        }
        else
        {
            await service.MarkChatReadByCustomerAsync(gmachId, ActorId, input.ThroughId, ct);
        }
        return NoContent();
    }

    private static (string, int, int, bool) TypingKey(int gmachId, int customerId, bool asOwner)
        => ("GmachTyping", gmachId, customerId, asOwner);

    [HttpPost("{gmachId:int}/typing")]
    public async Task<IActionResult> Typing(int gmachId, [FromQuery] int? customerId, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        if (gmachId <= 0 || customerId <= 0) return BadRequest();
        if (customerId.HasValue && !User.IsInRole("GmachAdmin")) return Forbid();
        if (!await service.CanAccessChatAsync(gmachId, customerId, ActorId, ct)) return NotFound();
        cache.Set(TypingKey(gmachId, customerId ?? ActorId, customerId.HasValue), true, TimeSpan.FromSeconds(7));
        return NoContent();
    }

    private const int MaxAttachmentSize = 1024 * 1024;
    public sealed record NewChatMessage([StringLength(1000)] string? Text, [Range(1, int.MaxValue)] int? ItemId,
        [StringLength(150)] string? AttachmentName, [StringLength(1_398_104)] string? AttachmentBase64);

    [HttpGet("{gmachId:int}/messages/{messageId:int}/attachment")]
    public async Task<IActionResult> DownloadAttachment(int gmachId, int messageId, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        var attachment = await service.GetAttachmentAsync(gmachId, messageId, ActorId, ct);
        if (attachment is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(attachment.Content, "application/octet-stream", attachment.Name);
    }

    [RequestSizeLimit(1_420_000), EnableRateLimiting("gmach-chat-send"), HttpPost("{gmachId:int}/messages")]
    public async Task<IActionResult> Send(int gmachId, [FromQuery] int? customerId, NewChatMessage input, CancellationToken ct)
    {
        if (ActorId <= 0) return Unauthorized();
        var message = input.Text?.Trim() ?? "";
        var hasAttachment = input.AttachmentName != null || input.AttachmentBase64 != null;
        if ((input.ItemId.HasValue && (customerId.HasValue || message.Length != 0 || hasAttachment)) ||
            (!input.ItemId.HasValue && message.Length == 0 && !hasAttachment))
            return BadRequest(new { message = "יש לכתוב הודעה, לצרף קובץ או לבחור פריט." });

        var attachment = hasAttachment ? ParseAttachment(input) : null;
        if (hasAttachment && attachment is null)
            return BadRequest(new { message = "אפשר לצרף תמונת JPG או PNG או קובץ PDF עד 1 MB." });

        var saved = await service.AddMessageAsync(gmachId, customerId, ActorId, message, input.ItemId, attachment, ct);
        if (!saved.Saved) return NotFound();
        cache.Remove(TypingKey(gmachId, customerId ?? ActorId, customerId.HasValue));
        if (saved.NotificationEmail is { } recipient)
            await NotifyChatRecipientAsync(recipient, saved.ToCustomer);
        return NoContent();
    }

    private static ChatAttachment? ParseAttachment(NewChatMessage input)
    {
        if (string.IsNullOrWhiteSpace(input.AttachmentName) || string.IsNullOrEmpty(input.AttachmentBase64))
            return null;
        var name = Path.GetFileName(input.AttachmentName.Replace('\\', '/')).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl)) return null;

        byte[] content;
        try { content = Convert.FromBase64String(input.AttachmentBase64); }
        catch (FormatException) { return null; }
        if (content.Length > MaxAttachmentSize) return null;

        var valid = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => content.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff }),
            ".png" => content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".pdf" => content.AsSpan().StartsWith("%PDF-"u8),
            _ => false
        };
        return valid ? new ChatAttachment(name, content) : null;
    }

    private async Task NotifyChatRecipientAsync(string recipientEmail, bool toCustomer)
    {
        try
        {
            await email.SendAsync(recipientEmail, toCustomer ? "מנהל הגמ״ח השיב לך בצ׳אט" : "הודעה חדשה בצ׳אט הגמ״ח",
                toCustomer ? "<div dir='rtl'><p>קיבלת תשובה חדשה ממנהל הגמ״ח.</p><p><a href='https://gmachmanagement-client.pages.dev/'>לצפייה בהודעה באתר</a></p></div>" : "<div dir='rtl'><p>ממתינה לך הודעה חדשה בצ׳אט.</p><p><a href='https://gmachmanagement-client.pages.dev/?page=owner'>כניסה לניהול הגמ״ח</a></p></div>",
                HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Chat notification email delivery failed");
        }
    }
}
