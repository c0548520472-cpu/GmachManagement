using Microsoft.AspNetCore.Mvc;

using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto request,
            CancellationToken cancellationToken)
        {
            var token = await _authService.LoginAsync(
                request,
                cancellationToken);

            if (token == null)
            {
                return Unauthorized(new
                {
                    message = "כתובת האימייל או הסיסמה שגויים"
                });
            }

            return Ok(new
            {
                accessToken = token,
                tokenType = "Bearer"
            });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto request,
            CancellationToken cancellationToken)
        {
            if (request.IsGmachOwner && (string.IsNullOrWhiteSpace(request.GmachName) || string.IsNullOrWhiteSpace(request.City)))
                return BadRequest(new { message = "יש להזין שם גמ״ח ועיר." });
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
                return Conflict(new { message = "כתובת האימייל כבר רשומה במערכת. ניתן לשחזר את הסיסמה באמצעות 'שכחתי סיסמה' במסך ההתחברות." });
            }

            if (!registered)
            {
                return Conflict(new
                {
                    message = "כתובת האימייל כבר רשומה במערכת. ניתן לשחזר את הסיסמה באמצעות 'שכחתי סיסמה' במסך ההתחברות."
                });
            }

            return StatusCode(201, new
            {
                message = "ההרשמה בוצעה בהצלחה"
            });
        }

    }
}

