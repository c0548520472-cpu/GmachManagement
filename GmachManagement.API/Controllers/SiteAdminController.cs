using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/site-admin")]
[Authorize(Roles="SiteAdmin")]
public sealed class SiteAdminController(SiteAdminService service,IConfiguration config) : ControllerBase {
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
 [HttpGet("users")]
 public async Task<IActionResult> Users(CancellationToken ct){
  if(!await Allowed(ct))return Forbid();
  var list=await service.UsersAsync(ct);
  return Ok(list.Select(u=>new {u.Id,u.Name,u.Email,role=(Bootstrap!=null && u.Email.Equals(Bootstrap,StringComparison.OrdinalIgnoreCase))?"SiteAdmin":u.Role.ToString(),isPrimary=Bootstrap!=null && u.Email.Equals(Bootstrap,StringComparison.OrdinalIgnoreCase)}));
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
  return Ok((await service.GmachsAsync(ct)).Select(g=>new {g.Id,g.Name,g.City,g.Description,g.OwnerId}));
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
  return Ok((await service.ItemsAsync(ct)).Select(i=>new {i.Id,i.Name,i.Description,i.GmachId,i.IsActive}));
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
}