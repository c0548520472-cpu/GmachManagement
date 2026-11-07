using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace GmachManagement.API.Controllers;
[ApiController, Route("api/gmachs")]
public sealed class GmachsController : ControllerBase {
 private readonly GmachService service;
 public GmachsController(GmachService service)=>this.service=service;
 private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private static object Project(Gmach g)=>new {g.Id,g.Name,g.City,g.Description,Items=g.Items.Select(i=>new {i.Id,i.Name,i.Description,i.IsActive,AvailableCopies=i.Copies.Count(c=>c.Status==GmachManagement.Core.Enums.ItemStatus.Available)})};
 [HttpGet,AllowAnonymous] public async Task<IActionResult> Search([FromQuery]string? city,[FromQuery]string? search,CancellationToken ct)=>Ok((await service.SearchAsync(city,search,ct)).Select(g=>new {g.Id,g.Name,g.City,g.Description}));
 [HttpGet("{id:int}"),AllowAnonymous] public async Task<IActionResult> Details(int id,CancellationToken ct){var g=await service.GetAsync(id,ct);return g==null?NotFound():Ok(Project(g));}
 [HttpPost("mine"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> CreateMine([FromBody]GmachDto dto,CancellationToken ct){
  if(await service.GetMineAsync(UserId,ct)!=null)return Conflict(new {message="כבר קיים גמ״ח המשויך לחשבון."});
  var g=await service.CreateAsync(new Gmach{Name=dto.Name.Trim(),City=dto.City.Trim(),Description=dto.Description,OwnerId=UserId},ct);
  return Created($"/api/gmachs/{g.Id}",new {g.Id,g.Name,g.City,g.Description});
 }
 [HttpGet("mine"),Authorize(Roles="GmachAdmin")] public async Task<IActionResult> Mine(CancellationToken ct){var g=await service.GetMineAsync(UserId,ct);return g==null?NotFound(new {message="לא נמצא גמ״ח משויך לחשבון."}):Ok(Project(await service.GetAsync(g.Id,ct)??g));}
 [HttpPost("mine/items"),Authorize(Roles="GmachAdmin")] public async Task<IActionResult> AddItem([FromBody]CreateItemDto dto,CancellationToken ct){var g=await service.GetMineAsync(UserId,ct);if(g==null)return Forbid();var item=await service.AddItemAsync(g.Id,dto.Name.Trim(),dto.Description?.Trim() ?? string.Empty,ct);return Created($"/api/items/{item.Id}",new {item.Id,item.Name,item.Description});}
}
