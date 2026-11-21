using GmachManagement.Core.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using GmachManagement.Core.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/auth/alternative-google-sessions")]
public sealed class GoogleSignInController(
    IConfiguration configuration, IDataProtectionProvider protection, IAuthService auth) : ControllerBase
{
    private string? ClientId => configuration["GoogleAuth:ClientId"]?.Trim();
    private ITimeLimitedDataProtector NonceProtector => protection.CreateProtector("Gmach.GoogleAuth.Nonce.v1").ToTimeLimitedDataProtector();

    [AllowAnonymous, HttpGet("configuration")]
    public IActionResult Config()
    {
        if (string.IsNullOrEmpty(ClientId)) return NoContent();
        Response.Headers.CacheControl = "no-store";
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        return Ok(new GoogleSignInConfigurationDto(ClientId, NonceProtector.Protect(nonce, TimeSpan.FromMinutes(20))));
    }

    public sealed record GoogleCredential(
        [Required, StringLength(8000)] string Credential, [Required, StringLength(1000)] string Nonce);

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> SignIn(GoogleCredential input, CancellationToken ct)
    {
        var clientId = ClientId;
        if (string.IsNullOrEmpty(clientId)) return NotFound();

        GoogleJsonWebSignature.Payload identity;
        try
        {
            NonceProtector.Unprotect(input.Nonce);
            identity = await GoogleJsonWebSignature.ValidateAsync(input.Credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(input.Credential);
            if (jwt.Claims.FirstOrDefault(claim => claim.Type == "nonce")?.Value != input.Nonce)
                return Unauthorized(new { message = "החיבור ל־Google פג. רענן את הדף ונסה שוב." });
        }
        catch (Exception ex) when (ex is InvalidJwtException or CryptographicException or ArgumentException)
        {
            return Unauthorized(new { message = "לא ניתן לאמת את הכניסה דרך Google. רענן את הדף ונסה שוב." });
        }

        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email) ||
            identity.Email.Length > 254 || string.IsNullOrWhiteSpace(identity.Subject) || identity.Subject.Length > 255)
            return Unauthorized(new { message = "נדרש חשבון Google עם כתובת אימייל מאומתת." });

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? (int?)id : null;
        try
        {
            var result = await auth.SignInWithGoogleAsync(identity.Subject, identity.Email, identity.Name ?? "", userId, ct);
            return result.Token == null
                ? Conflict(new { message = result.Error })
                : Ok(new SessionDto(result.Token));
        }
        catch (DbException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { message = "חשבון Google או כתובת האימייל כבר מקושרים. יש לנסות להתחבר שוב." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is DbException { SqlState: "23505" })
        {
            return Conflict(new { message = "כתובת האימייל כבר רשומה. יש להתחבר לחשבון הקיים כדי לקשר אותו." });
        }
    }
}
