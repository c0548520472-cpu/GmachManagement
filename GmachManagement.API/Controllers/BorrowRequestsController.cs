using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController, Route("api/borrow-requests")]
public class BorrowRequestsController : ControllerBase {
 private readonly GmachService service;
 public BorrowRequestsController(GmachService service)=>this.service=service;
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
  return result==null?Conflict(new{message="אין עותק זמין לבקשה."}):StatusCode(201,new{message="הבקשה נשלחה בהצלחה! בעל הגמ״ח יצור איתך קשר."});
 }
 [HttpGet("mine"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> Mine(CancellationToken ct){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var ownerId))return Unauthorized();
  var requests=await service.GetBorrowRequestsAsync(ownerId,ct);
  return Ok(requests.Select(r=>new{r.Id,r.ItemId,ItemName=r.Item.Name,r.ApplicantName,r.Phone,r.Email,r.CreatedAt}));
 }
 [HttpPost("mine/{id:int}/fulfill"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> Fulfill(int id,CancellationToken ct){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var ownerId))return Unauthorized();
  return await service.FulfillBorrowRequestAsync(ownerId,id,ct)?NoContent():Conflict(new{message="הבקשה או הפריט אינם זמינים."});
 }
}
