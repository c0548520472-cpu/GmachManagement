using Microsoft.AspNetCore.Mvc;

using GmachManagement.Core.DTOs;
using GmachManagement.API.Services;
using GmachManagement.Service.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace GmachManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _users;
        private readonly PasswordService _passwords;
        private readonly ResendEmailService _email;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IUserRepository users, PasswordService passwords, ResendEmailService email, ILogger<AuthController> logger)
        {
            _authService = authService;
            _users = users;
            _passwords = passwords;
            _email = email;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("sessions")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto request,
            CancellationToken cancellationToken)
        {
            var token = await _authService.LoginAsync(
                request,
                cancellationToken);

            if (token == null)
            {
                if (await _users.RequiresReRegistrationApprovalAsync(request.Email, cancellationToken))
                    return StatusCode(403, new
                    {
                        message = "החשבון שלך נמחק על ידי מנהל האתר. כדי להשתמש באתר שוב, יש להירשם מחדש לאחר קבלת אישור מנהל האתר.",
                        requiresApproval = true,
                        accountDeleted = true
                    });
                return Unauthorized(new
                {
                    message = "כתובת האימייל או הסיסמה שגויים"
                });
            }

            return Ok(new SessionDto(token));
        }

        [AllowAnonymous]
        [HttpPost("accounts")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto request,
            CancellationToken cancellationToken)
        {
            if(await _authService.RequiresReRegistrationApprovalAsync(request.Email,cancellationToken))
                return StatusCode(403,new {message="החשבון הוסר בעבר. יש לבקש אישור מנהל לפני הרשמה מחדש.",requiresApproval=true});
            bool registered;
            try
            {
                registered = await _authService.RegisterAsync(request, cancellationToken);
            }
            catch (DbUpdateException ex) when (
                (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627)) ||
                (ex.InnerException?.GetType().Name == "PostgresException" &&
                 ex.InnerException.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException) as string == "23505"))
            {
                return Conflict(new { message = "כתובת האימייל כבר רשומה במערכת. אפשר לעדכן פרטים לאחר התחברות." });
            }

            if (!registered)
            {
                return Conflict(new
                {
                    message = "כתובת האימייל כבר רשומה במערכת. אפשר לעדכן פרטים לאחר התחברות."
                });
            }

            return CreatedAtAction(nameof(Profile), new
            {
                message = "ההרשמה בוצעה בהצלחה."
            });
        }



        public sealed record GoogleLoginRequest(
            [System.ComponentModel.DataAnnotations.Required] string Credential,
            [System.ComponentModel.DataAnnotations.Required] string Nonce);

        [AllowAnonymous, HttpGet("google-sign-in/configuration")]
        public IActionResult GoogleConfig([FromServices] IConfiguration configuration,
            [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            Response.Headers.CacheControl = "no-store";
            var clientId = configuration["Google:ClientId"]?.Trim();
            if (string.IsNullOrWhiteSpace(clientId))
                return StatusCode(503, new { message = "התחברות באמצעות Google טרם הוגדרה. יש להוסיף Google__ClientId ב־Render." });
            try
            {
                var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                using (var entry = cache.CreateEntry("google-nonce:" + nonce))
                {
                    entry.Value = true;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }
                return Ok(new GoogleSignInConfigurationDto(clientId, nonce));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google sign-in configuration failed");
                return StatusCode(503, new { message = "התחברות Google אינה זמינה כרגע. נסו שוב מאוחר יותר." });
            }
        }

        [AllowAnonymous, HttpPost("google-sessions")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("password-recovery")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request,
            [FromServices] IConfiguration configuration,
            [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
            CancellationToken ct)
        {
            var clientId = configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId)) return StatusCode(503, new { message = "התחברות Google אינה מוגדרת." });
            if (!cache.TryGetValue("google-nonce:" + request.Nonce, out _))
                return BadRequest(new { message = "תוקף בקשת ההתחברות פג. יש לנסות שוב." });
            cache.Remove("google-nonce:" + request.Nonce);
            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(
                    request.Credential,
                    new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                    { Audience = new[] { clientId } });
            }
            catch (Exception ex) when (ex is Google.Apis.Auth.InvalidJwtException or ArgumentException or HttpRequestException)
            {
                return Unauthorized(new { message = "אימות Google נכשל. נסו שוב." });
            }
            if (!string.Equals(payload.Nonce, request.Nonce, StringComparison.Ordinal) ||
                payload.EmailVerified != true || string.IsNullOrWhiteSpace(payload.Email) ||
                string.IsNullOrWhiteSpace(payload.Subject))
                return Unauthorized(new { message = "פרטי האימות של Google אינם תקינים." });
            var actorId = User.Identity?.IsAuthenticated == true &&
                int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
                ? (int?)id : null;
            var result = await _authService.SignInWithGoogleAsync(
                payload.Subject, payload.Email, payload.Name ?? "", actorId, ct);
            return result.Token is { } accessToken
                ? Ok(new SessionDto(accessToken))
                : Conflict(new { message = result.Error });
        }

        public sealed record ForgotPasswordRequest([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress] string Email);
        public sealed record ResetPasswordRequest([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress] string Email,
            [System.ComponentModel.DataAnnotations.Required] string Token,
            [System.ComponentModel.DataAnnotations.Required] string NewPassword);

        [AllowAnonymous, HttpPost("password-recovery-requests")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("password-recovery")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            const string message = "אם קיים חשבון עם כתובת המייל הזו, תישלח אליו הודעה עם קישור לאיפוס הסיסמה.";
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email, ct);
            if (user == null) return Ok(new { message });

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expires = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            await _users.SetRecoveryCodeHashAsync(user.Id, $"{expires}:{tokenHash}", ct);
            var url = $"https://gmachmanagement-client.pages.dev/?resetEmail={Uri.EscapeDataString(email)}&resetToken={Uri.EscapeDataString(token)}";
            try
            {
                await _email.SendAsync(email, "איפוס סיסמה - ניהול הגמ״ח",
                    $"<div dir='rtl'><p>התקבלה בקשה לאיפוס הסיסמה שלך.</p><p><a href='{HtmlEncoder.Default.Encode(url)}'>לחצי כאן לקביעת סיסמה חדשה</a></p><p>הקישור בתוקף למשך 30 דקות. אם לא ביקשת איפוס, אפשר להתעלם מההודעה.</p></div>", ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                _logger.LogError(ex, "Failed to send password reset email");
            }
            return Ok(new { message });
        }

        [AllowAnonymous, HttpPost("password-resets")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("password-recovery")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            if (request.NewPassword.Length is < 8 or > 128)
                return BadRequest(new { message = "הסיסמה החדשה חייבת להכיל 8 עד 128 תווים." });
            if (request.Token.Length != 64 || !request.Token.All(Uri.IsHexDigit))
                return BadRequest(new { message = "קישור האיפוס אינו תקין או שפג תוקפו." });
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email, ct);
            if (user?.RecoveryCodeHash == null)
                return BadRequest(new { message = "קישור האיפוס אינו תקין או שפג תוקפו." });
            var parts = user.RecoveryCodeHash.Split(':', 2);
            if (parts.Length != 2 || !long.TryParse(parts[0], out var expires) || expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return BadRequest(new { message = "קישור האיפוס אינו תקין או שפג תוקפו." });
            var suppliedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(parts[1]), Encoding.ASCII.GetBytes(suppliedHash)))
                return BadRequest(new { message = "קישור האיפוס אינו תקין או שפג תוקפו." });
            var newHash = _passwords.HashPassword(user, request.NewPassword);
            if (!await _users.ConsumeRecoveryCodeAsync(email, user.RecoveryCodeHash, newHash, ct))
                return BadRequest(new { message = "קישור האיפוס כבר נוצל. יש לבקש קישור חדש." });
            return Ok(new { message = "הסיסמה עודכנה. אפשר להתחבר כעת." });
        }

        public sealed record ReRegistrationRequest([System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.EmailAddress] string Email);
        [AllowAnonymous,HttpPost("re-registration-requests")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("password-recovery")]
        public async Task<IActionResult> RequestReRegistration([FromBody] ReRegistrationRequest request,CancellationToken ct){
            var saved = await _authService.RequestReRegistrationAsync(request.Email,ct);
            if (saved)
            {
                var adminEmail = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["SiteAdmin:BootstrapEmail"];
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    try
                    {
                        await _email.SendAsync(adminEmail, "בקשה לאישור הרשמה מחדש באתר ניהול הגמ״ח",
                            "<div dir='rtl'><p>התקבלה בקשה לאישור הרשמה מחדש עבור החשבון: " +
                            HtmlEncoder.Default.Encode(request.Email.Trim()) +
                            "</p><p><a href='https://gmachmanagement-client.pages.dev/?page=siteAdmin'>כניסה לניהול האתר</a></p></div>", ct);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
                    {
                        _logger.LogWarning(ex, "Re-registration approval email notification failed");
                    }
                }
            }
            return Ok(new {message="אם החשבון הוסר בעבר, בקשת האישור נשמרה. מנהל האתר יוכל לטפל בה."});
        }
        public sealed class ProfileUpdateRequest {
            [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.StringLength(150)] public string Name {get;set;}="";
            [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.EmailAddress] public string Email {get;set;}="";
            [System.ComponentModel.DataAnnotations.Required] public string CurrentPassword {get;set;}="";
            public string? NewPassword {get;set;}
            public bool IsGmachOwner {get;set;}
            public string? GmachName {get;set;}
            public string? City {get;set;}
            public string? GmachType {get;set;}
        }
        [Authorize,HttpGet("profiles/current")]
        public async Task<IActionResult> Profile(CancellationToken ct){
            if(!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id))return Unauthorized();
            var p=await _authService.GetProfileAsync(id,ct);
            return p.Name==null?NotFound():Ok(new AccountProfileDto(p.Name, p.Email, p.IsOwner, p.GmachName, p.City, p.GmachType));
        }
        [Authorize,HttpPut("profiles/current")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request,CancellationToken ct){
            if(!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id))return Unauthorized();
            var result=await _authService.UpdateProfileAsync(id,request.Name,request.Email,request.CurrentPassword,request.NewPassword,request.IsGmachOwner,request.GmachName,request.City,request.GmachType,ct);
            return result.Error==null?Ok(new ProfileUpdateDto(result.Token,"הפרטים עודכנו בהצלחה.")):BadRequest(new {message=result.Error});
        }

}
}
