using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace GmachManagement.API.Controllers;
[ApiController]
[Route("api/site-activity")]
public sealed class SiteActivityController(IActivityRepository repository,SiteAdminService admins,IConfiguration config,ILogger<SiteActivityController> logger):ControllerBase {
 private static readonly HashSet<string> AllowedEvents=new(StringComparer.Ordinal){
  "page_view","search","gmach_view","item_view","borrow_click","login_success","registration_success"
 };
 public sealed record ActivityInput([Required] string SessionId,[Required] string EventType,string? Detail);
 [HttpPost]
 [AllowAnonymous]
 [EnableRateLimiting("site-activity")]
 public async Task<IActionResult> Record(ActivityInput input,CancellationToken ct){
  if(input.SessionId.Length>64 || !Guid.TryParse(input.SessionId,out _) || !AllowedEvents.Contains(input.EventType) || (input.Detail?.Length??0)>180)
   return BadRequest(new {message="אירוע פעילות לא תקין."});
  var id=int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor)?actor:(int?)null;
  try {
   await repository.RecordAsync(id,input.SessionId,input.EventType,input.Detail??"",ct);
   return NoContent();
  } catch(Exception ex) {
   logger.LogWarning(ex,"Site activity recording failed");
   return StatusCode(503,new {message="רישום הפעילות אינו זמין כרגע."});
  }
 }
 [HttpGet]
 [Authorize(Roles="SiteAdmin")]
 public async Task<IActionResult> Recent([FromQuery] int? userId,[FromQuery] string? eventType,[FromQuery] int limit=100,CancellationToken ct=default){
  if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor) ||
   !await admins.IsAdminAsync(actor,config["SiteAdmin:BootstrapEmail"],ct))return Forbid();
  if(eventType!=null&&!AllowedEvents.Contains(eventType))return BadRequest(new {message="סוג פעילות לא תקין."});
  var entries=await repository.RecentAsync(userId,eventType,Math.Clamp(limit,1,300),ct);
  return Ok(entries);
 }
}
