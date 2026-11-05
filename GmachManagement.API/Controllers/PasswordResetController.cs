using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/Auth")]
public sealed class PasswordResetController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly PasswordService _passwords;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IConfiguration _config;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(IUserRepository users, PasswordService passwords,
        IDataProtectionProvider protection, IConfiguration config, ILogger<PasswordResetController> logger)
    {
        _users = users;
        _passwords = passwords;
        _protector = protection.CreateProtector("GmachManagement.PasswordReset.v1").ToTimeLimitedDataProtector();
        _config = config;
        _logger = logger;
    }

    public sealed record ForgotRequest(string Email);
    public sealed record ResetRequest(string Token, string NewPassword);
    private sealed record Payload(int UserId, string PasswordHash);

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot([FromBody] ForgotRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "יש להזין כתובת אימייל" });
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user != null)
        {
            var host = _config["Email:SmtpHost"];
            var from = _config["Email:From"];
            var clientUrl = _config["PasswordReset:ClientUrl"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(clientUrl))
                return StatusCode(503, new { message = "שירות שליחת האימייל עדיין לא הוגדר" });

            var token = _protector.Protect(JsonSerializer.Serialize(new Payload(user.Id, user.PasswordHash)), TimeSpan.FromMinutes(30));
            var link = clientUrl.TrimEnd('/') + "/?resetToken=" + Uri.EscapeDataString(token);
            using var mail = new MailMessage(from, user.Email)
            {
                Subject = "איפוס סיסמה - ניהול הגמ״ח",
                Body = "לאיפוס הסיסמה לחצו על הקישור (תקף ל-30 דקות):\n" + link + "\nאם לא ביקשתם איפוס, התעלמו מההודעה."
            };
            using var smtp = new SmtpClient(host, int.TryParse(_config["Email:SmtpPort"], out var port) ? port : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
            };
            try { await smtp.SendMailAsync(mail, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset email delivery failed");
                return StatusCode(503, new { message = "שליחת האימייל נכשלה. נסו שוב מאוחר יותר." });
            }
        }
        return Ok(new { message = "אם האימייל רשום במערכת, יישלח אליו קישור לאיפוס." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] ResetRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { message = "נדרש קישור תקין וסיסמה חדשה באורך 8 תווים לפחות" });
        Payload? payload;
        try { payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(request.Token)); }
        catch (CryptographicException) { return BadRequest(new { message = "קישור האיפוס פג תוקף או אינו תקין" }); }
        if (payload == null) return BadRequest(new { message = "קישור לא תקין" });
        var user = await _users.GetByIdAsync(payload.UserId, ct);
        if (user == null || user.PasswordHash != payload.PasswordHash)
            return BadRequest(new { message = "קישור האיפוס כבר נוצל או אינו תקין" });
        var newHash = _passwords.HashPassword(user, request.NewPassword);
        if (!await _users.UpdatePasswordHashAsync(user.Id, payload.PasswordHash, newHash, ct))
            return BadRequest(new { message = "קישור האיפוס כבר נוצל" });
        return Ok(new { message = "הסיסמה עודכנה בהצלחה" });
    }
}
