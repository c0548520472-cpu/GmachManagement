using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GmachManagement.Service.Services;
using GmachManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController, Route("api/borrow-requests")]
public class BorrowRequestsController : ControllerBase {
 private readonly GmachService service;
 private readonly ResendEmailService email;
 private readonly ILogger<BorrowRequestsController> logger;
 public BorrowRequestsController(GmachService service,ResendEmailService email,ILogger<BorrowRequestsController> logger){this.service=service;this.email=email;this.logger=logger;}
 public sealed class NewRequest : IValidatableObject {
  [Range(1,int.MaxValue)] public int ItemId {get;set;}
  [Required, StringLength(150,MinimumLength=2)] public string ApplicantName {get;set;}="";
  [Phone, StringLength(30)] public string? Phone {get;set;}
  [EmailAddress, StringLength(254)] public string? Email {get;set;}
  public IEnumerable<ValidationResult> Validate(ValidationContext context) {
   if(string.IsNullOrWhiteSpace(Phone)&&string.IsNullOrWhiteSpace(Email))
    yield return new ValidationResult("יש להזין טלפון או אימייל",new[]{nameof(Phone),nameof(Email)});
  }
 }
 [HttpPost,AllowAnonymous]
 public async Task<IActionResult> Create([FromBody] NewRequest request,CancellationToken ct){
  var applicantId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? (int?)userId : null;
  var result=await service.AddBorrowRequestAsync(request.ItemId,request.ApplicantName,request.Phone,request.Email,ct,applicantId);
  if (result == null)
  {
   logger.LogWarning("No copy available for borrow request on item {ItemId}", request.ItemId);
   return Conflict(new { message = "אין עותק זמין לבקשה." });
  }
  try {
   var ownerEmail=await service.GetOwnerEmailForItemAsync(request.ItemId,ct);
   if(!string.IsNullOrWhiteSpace(ownerEmail))
    await email.SendAsync(ownerEmail,"בקשת השאלה חדשה - ניהול הגמ״ח",
     "<div dir='rtl'><p>התקבלה בקשת השאלה חדשה באתר.</p><p>היכנסו לאזור ניהול הגמ״ח כדי לצפות בפרטים ולטפל בבקשה.</p><p><a href='https://gmachmanagement-client.pages.dev/'>כניסה לאתר</a></p></div>",ct);
  } catch(Exception ex) when(ex is HttpRequestException or InvalidOperationException or TaskCanceledException){
   logger.LogWarning(ex,"Borrow request saved but owner notification email failed");
  }
  return CreatedAtAction(nameof(GetMineById),new{id=result.Id},new{message="הבקשה נשלחה בהצלחה! בעל הגמ״ח יצור איתך קשר."});
 }
 [HttpGet("mine/{id:int}"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> GetMineById([FromRoute] int id,CancellationToken ct){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var ownerId))return Unauthorized();
  var requests=await service.GetBorrowRequestsAsync(ownerId,ct);
  var request=requests.FirstOrDefault(r=>r.Id==id);
  return request==null?NotFound():Ok(new{request.Id,request.ItemId,ItemName=request.Item.Name,request.ApplicantName,request.Phone,request.Email,request.CreatedAt});
 }
 [HttpGet("mine"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> Mine(CancellationToken ct){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var ownerId))return Unauthorized();
  var requests=await service.GetBorrowRequestsAsync(ownerId,ct);
  return Ok(requests.Select(r=>new{r.Id,r.ItemId,ItemName=r.Item.Name,r.ApplicantName,r.Phone,r.Email,r.CreatedAt}));
 }
 [HttpPost("mine/{id:int}/fulfill"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> Fulfill([FromRoute] int id,CancellationToken ct){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var ownerId))return Unauthorized();
  if (await service.FulfillBorrowRequestAsync(ownerId, id, ct)) return NoContent();
  logger.LogWarning("Borrow request {RequestId} or its item is unavailable for fulfillment", id);
  return Conflict(new { message = "הבקשה או הפריט אינם זמינים." });
 }
}
