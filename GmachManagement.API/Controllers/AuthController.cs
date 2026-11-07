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
                message = "ההרשמה בוצעה בהצלחה."
            });
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
        [Authorize,HttpGet("profile")]
        public async Task<IActionResult> Profile(CancellationToken ct){
            if(!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id))return Unauthorized();
            var p=await _authService.GetProfileAsync(id,ct);
            return p.Name==null?NotFound():Ok(new {name=p.Name,email=p.Email,isGmachOwner=p.IsOwner,gmachName=p.GmachName,city=p.City,gmachType=p.GmachType});
        }
        [Authorize,HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request,CancellationToken ct){
            if(!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id))return Unauthorized();
            var result=await _authService.UpdateProfileAsync(id,request.Name,request.Email,request.CurrentPassword,request.NewPassword,request.IsGmachOwner,request.GmachName,request.City,request.GmachType,ct);
            return result.Error==null?Ok(new {accessToken=result.Token,message="הפרטים עודכנו בהצלחה."}):BadRequest(new {message=result.Error});
        }

}

