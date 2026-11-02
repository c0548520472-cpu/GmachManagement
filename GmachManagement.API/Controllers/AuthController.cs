using Microsoft.AspNetCore.Mvc;

using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
            var registered = await _authService.RegisterAsync(
                request,
                cancellationToken);

            if (!registered)
            {
                return Conflict(new
                {
                    message = "כתובת האימייל כבר רשומה במערכת"
                });
            }

            return StatusCode(201, new
            {
                message = "ההרשמה בוצעה בהצלחה"
            });
        }

    }
}

