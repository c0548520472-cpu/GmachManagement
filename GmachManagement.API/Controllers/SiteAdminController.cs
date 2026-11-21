using GmachManagement.Core.DTOs;
using GmachManagement.Service.Services;
using GmachManagement.API.Services;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/site-admin")]
[Authorize(Roles="SiteAdmin")]
public sealed class SiteAdminController(SiteAdminService service, AutoMapper.IMapper mapper, IConfiguration config,ResendEmailService emailService,ILogger<SiteAdminController> logger) : ControllerBase {
 private string? Bootstrap => config["SiteAdmin:BootstrapEmail"];
 private async Task<bool> Allowed(CancellationToken ct)=>
  int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) &&
  await service.IsAdminAsync(id,Bootstrap,ct);
 private int ActorId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private IActionResult Result(string? error)=>error==null?NoContent():BadRequest(new {message=error});
 public sealed record EmailInput([Required,EmailAddress] string Email);
 public sealed record AdminGrantInput([Required,EmailAddress] string Email,bool Enabled);
 public sealed record UserEditInput([Required,StringLength(150,MinimumLength=1)] string Name);
 public sealed record GmachEditInput([Required,StringLength(150,MinimumLength=1)] string Name,[Required,StringLength(100,MinimumLength=1)] string City,string? Description);
 public sealed record ItemEditInput([Required,StringLength(150,MinimumLength=1)] string Name,string? Description);
 [HttpGet("re-registration-requests")]
 public async Task<IActionResult> ReRegistrationRequests(CancellationToken ct){if(!await Allowed(ct))return Forbid();return Ok(mapper.Map<List<ReRegistrationRequestDto>>(await service.PendingReRegistrationsAsync(ct)));}
 [HttpPost("re-registration-requests/approvals")]
 public async Task<IActionResult> ApproveReRegistration(EmailInput input,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  var error=await service.ApproveReRegistrationAsync(input.Email,ct);
  if(error!=null)return BadRequest(new {message=error});
  var notified=await NotifyReRegistrationDecisionAsync(input.Email,true,ct);
  return Ok(new {message=notified?"ההרשמה מחדש אושרה ונשלחה הודעה למשתמש.":"ההרשמה מחדש אושרה, אך שליחת המייל נכשלה.",emailSent=notified});
 }
 [HttpPost("re-registration-requests/rejections")]
 public async Task<IActionResult> RejectReRegistration(EmailInput input,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  var error=await service.RejectReRegistrationAsync(input.Email,ct);
  if(error!=null)return BadRequest(new {message=error});
  var notified=await NotifyReRegistrationDecisionAsync(input.Email,false,ct);
  return Ok(new {message=notified?"הבקשה נדחתה ונשלחה הודעה למשתמש.":"הבקשה נדחתה, אך שליחת המייל נכשלה.",emailSent=notified});
 }
 private async Task<bool> NotifyReRegistrationDecisionAsync(string recipient,bool approved,CancellationToken ct){
  var subject=approved?"בקשתך להרשמה מחדש אושרה – ניהול הגמ״ח":"עדכון לגבי בקשת ההרשמה מחדש – ניהול הגמ״ח";
  var html=approved
   ? "<div dir='rtl'><h2>בקשתך אושרה</h2><p>מנהל האתר אישר לך להירשם מחדש לאתר ניהול הגמ״ח.</p><p>כעת אפשר לבצע הרשמה חדשה עם כתובת האימייל שלך.</p><p><a href='https://gmachmanagement-client.pages.dev/?page=login'>מעבר לאתר</a></p></div>"
   : "<div dir='rtl'><h2>עדכון לגבי בקשתך</h2><p>מנהל האתר דחה את בקשתך להרשמה מחדש לאתר ניהול הגמ״ח.</p><p>בשלב זה לא ניתן להירשם מחדש עם החשבון הזה.</p></div>";
  try{
   await emailService.SendAsync(recipient.Trim(),subject,html,ct);
   return true;
  }catch(Exception ex) when(ex is not OperationCanceledException || !ct.IsCancellationRequested){
   logger.LogWarning(ex,"Re-registration decision email failed");
   return false;
  }
 }
 [HttpGet("users")]
 public async Task<IActionResult> Users(CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  var list=await service.UsersAsync(ct);
  return Ok(list.Select(user =>
  {
   var dto = mapper.Map<SiteAdminUserDto>(user);
   return string.Equals(user.Email, Bootstrap, StringComparison.OrdinalIgnoreCase)
    ? dto with { Role = "SiteAdmin", IsPrimary = true }
    : dto;
  }).ToList());
 }
 [HttpPost("administrators")]
 public async Task<IActionResult> SetAdmin(AdminGrantInput request,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  if(!request.Enabled && User.FindFirstValue(ClaimTypes.Email)?.Equals(request.Email,StringComparison.OrdinalIgnoreCase)==true)
   return BadRequest(new {message="לא ניתן להסיר הרשאה מהחשבון הנוכחי."});
  return Result(await service.SetAdminAsync(request.Email,request.Enabled,Bootstrap,ct));
 }
 [HttpPut("users/{id:int}/email")]
 public async Task<IActionResult> Email(int id,EmailInput request,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.ChangeEmailAsync(id,request.Email,Bootstrap,ct));
 }
 [HttpPut("users/{id:int}")]
 public async Task<IActionResult> UserEdit(int id,UserEditInput request,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.EditUserAsync(id,request.Name,ct));
 }
 [HttpDelete("users/{id:int}")]
 public async Task<IActionResult> UserDelete(int id,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.DeleteUserAsync(id,ActorId,Bootstrap,ct));
 }
 [HttpGet("gmachs")]
 public async Task<IActionResult> Gmachs(CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Ok(mapper.Map<List<SiteAdminGmachDto>>(await service.GmachsAsync(ct)));
 }
 [HttpPut("gmachs/{id:int}")]
 public async Task<IActionResult> GmachEdit(int id,GmachEditInput request,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.EditGmachAsync(id,request.Name,request.City,request.Description??"",ct));
 }
 [HttpDelete("gmachs/{id:int}")]
 public async Task<IActionResult> GmachDelete(int id,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.DeleteGmachAsync(id,ct));
 }
 [HttpGet("items")]
 public async Task<IActionResult> Items(CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Ok(mapper.Map<List<SiteAdminItemDto>>(await service.ItemsAsync(ct)));
 }
 [HttpPut("items/{id:int}")]
 public async Task<IActionResult> ItemEdit(int id,ItemEditInput request,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.EditItemAsync(id,request.Name,request.Description??"",ct));
 }
 [HttpDelete("items/{id:int}")]
 public async Task<IActionResult> ItemDelete(int id,CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  return Result(await service.DeleteItemAsync(id,ct));
 }

 public sealed record DeleteOpenLoansInput(bool Confirmed, bool All, OpenLoanKey[]? Loans);

 [HttpGet("open-loans")]
 public async Task<IActionResult> OpenLoans(CancellationToken ct)
 {
     if (!await Allowed(ct)) return Forbid();
     return Ok(await service.OpenLoansAsync(ct));
 }

 [HttpDelete("open-loans")]
 public async Task<IActionResult> DeleteOpenLoans(DeleteOpenLoansInput request, CancellationToken ct)
 {
     if (!await Allowed(ct)) return Forbid();

     if (!request.Confirmed || request.Loans is not { Length: > 0 } ||
         request.Loans.Any(key => key is null || key.Id <= 0 || key.Kind is not ("standard" or "managed")))
     {
         return BadRequest(new { message = "יש לבחור השאלות פתוחות ולאשר את המחיקה." });
     }

     var deleted = await service.DeleteOpenLoansAsync(request.Loans.Distinct().ToArray(), request.All, ct);
     return Ok(new OpenLoanDeletionResultDto(deleted));
 }
}
