using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
namespace GmachManagement.API.Controllers;
[ApiController, Route("api/gmachs")]
public sealed class GmachsController : ControllerBase {
 private readonly GmachService service;
 private readonly IMemoryCache cache;
 public GmachsController(GmachService service, IMemoryCache cache){this.service=service;this.cache=cache;}
 private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private static object Project(Gmach g)=>new {g.Id,g.Name,g.City,g.Description,Items=g.Items.Where(i=>i.IsActive).Select(i=>new {i.Id,i.Name,i.Description,i.IsActive,AvailableCopies=i.Copies.Count(c=>c.Status==GmachManagement.Core.Enums.ItemStatus.Available)})};
 [HttpGet,AllowAnonymous] public async Task<IActionResult> Search([FromQuery]string? city,[FromQuery]string? search,[FromQuery]string? itemSearch,CancellationToken ct){
  var key="public-gmachs:"+city?.Trim()+":"+search?.Trim()+":"+itemSearch?.Trim();
  var result=await cache.GetOrCreateAsync(key,async entry=>{
   entry.AbsoluteExpirationRelativeToNow=TimeSpan.FromSeconds(20);
   return (await service.SearchAsync(city,search,itemSearch,ct)).Select(g=>new {g.Id,g.Name,g.City,g.Description}).ToList();
  });
  return Ok(result);
 }
 [HttpGet("{id:int}"),AllowAnonymous] public async Task<IActionResult> Details(int id,CancellationToken ct){var g=await service.GetAsync(id,ct);return g==null?NotFound():Ok(Project(g));}
 [HttpPost("mine"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> CreateMine([FromBody]GmachDto dto,CancellationToken ct){
  if(await service.GetMineAsync(UserId,ct)!=null)return Conflict(new {message="כבר קיים גמ״ח המשויך לחשבון."});
  var g=await service.CreateAsync(new Gmach{Name=dto.Name.Trim(),City=dto.City.Trim(),Description=dto.Description,OwnerId=UserId},ct);
  return Created($"/api/gmachs/{g.Id}",new {g.Id,g.Name,g.City,g.Description});
 }
 [HttpGet("mine"),Authorize(Roles="GmachAdmin")] public async Task<IActionResult> Mine(CancellationToken ct){var g=await service.GetMineAsync(UserId,ct);return g==null?NotFound(new {message="לא נמצא גמ״ח משויך לחשבון."}):Ok(new {g.Id,g.Name,g.City,g.Description,Items=(await service.GetAsync(g.Id,ct)??g).Items.Select(i=>new {i.Id,i.Name,i.Description,i.IsActive,TotalCopies=i.Copies.Count,AvailableCopies=i.Copies.Count(c=>c.Status==GmachManagement.Core.Enums.ItemStatus.Available)})});}
 [HttpPost("mine/items"),Authorize(Roles="GmachAdmin")] public async Task<IActionResult> AddItem([FromBody]CreateItemDto dto,CancellationToken ct){var g=await service.GetMineAsync(UserId,ct);if(g==null)return Forbid();var item=await service.AddItemAsync(g.Id,dto.Name.Trim(),dto.Description?.Trim() ?? string.Empty,ct);return Created($"/api/items/{item.Id}",new {item.Id,item.Name,item.Description});}
 public sealed class MarkBorrowedRequest {
  [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)] public string BorrowerName {get;set;} = "";
  [System.ComponentModel.DataAnnotations.EmailAddress] public string? BorrowerEmail {get;set;}
  [System.ComponentModel.DataAnnotations.Phone] public string? BorrowerPhone {get;set;}
 }
 [HttpPost("mine/items/{itemId:int}/borrow"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> MarkBorrowed(int itemId,[FromBody] MarkBorrowedRequest request,CancellationToken ct){
  if(string.IsNullOrWhiteSpace(request.BorrowerName))return BadRequest(new {message="חובה למלא שם שואל."});
  if(string.IsNullOrWhiteSpace(request.BorrowerEmail)&&string.IsNullOrWhiteSpace(request.BorrowerPhone))return BadRequest(new {message="חובה למלא לפחות טלפון או אימייל."});
  var loan=await service.MarkBorrowedAsync(UserId,itemId,request.BorrowerName.Trim(),string.IsNullOrWhiteSpace(request.BorrowerPhone)?null:request.BorrowerPhone.Trim(),string.IsNullOrWhiteSpace(request.BorrowerEmail)?null:request.BorrowerEmail.Trim(),ct);
  return loan==null?Conflict(new {message="אין עותק זמין להשאלה."}):Ok(loan);
 }
 [HttpGet("mine/managed-loans"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> ManagedLoans(CancellationToken ct){
  var loans=await service.GetManagedLoansAsync(UserId,ct);
  var ownedGmach=await service.GetMineAsync(UserId,ct);
  var gmach=ownedGmach==null?null:await service.GetAsync(ownedGmach.Id,ct);
  var copies=(gmach?.Items??Enumerable.Empty<GmachManagement.Core.Entities.Item>())
   .SelectMany(item=>item.Copies.Select(copy=>new {copy.Id,ItemId=item.Id,ItemName=item.Name}))
   .ToDictionary(copy=>copy.Id);
  return Ok(loans.Select(loan=>new {
   loan.Id,loan.GmachId,loan.ItemCopyId,loan.BorrowerName,loan.BorrowerPhone,loan.BorrowerEmail,loan.BorrowedAt,loan.ReturnedAt,
   ItemId=copies.TryGetValue(loan.ItemCopyId,out var copy)?copy.ItemId:(int?)null,
   ItemName=copies.TryGetValue(loan.ItemCopyId,out var namedCopy)?namedCopy.ItemName:null
  }));
 }
 [HttpPost("mine/managed-loans/{id:int}/return"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> ReturnManagedLoan(int id,CancellationToken ct)=>await service.ReturnManagedLoanAsync(UserId,id,ct)?NoContent():NotFound();
}
