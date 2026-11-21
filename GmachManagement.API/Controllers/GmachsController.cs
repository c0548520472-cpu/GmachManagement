using AutoMapper;
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
 private readonly IMapper mapper;
 private readonly ILogger<GmachsController> logger;
 public GmachsController(GmachService service, IMemoryCache cache, IMapper mapper, ILogger<GmachsController> logger)
 {
  this.service = service;
  this.cache = cache;
  this.mapper = mapper;
  this.logger = logger;
 }
 private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet,AllowAnonymous]
 public async Task<IActionResult> Search(
  [FromQuery]string? city, [FromQuery]string? search, [FromQuery]string? itemSearch,
  [FromQuery]string? query, CancellationToken ct)
 {
  var term = query?.Trim();
  var cityTerm = city?.Trim();
  var key = ("public-gmachs", cityTerm, search?.Trim(), itemSearch?.Trim(), term);
  var result = await cache.GetOrCreateAsync(key, async entry =>
  {
   entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
   var gmachs = string.IsNullOrEmpty(term)
    ? await service.SearchAsync(city, search, itemSearch, ct)
    : await service.SearchDirectoryAsync(string.IsNullOrEmpty(cityTerm) ? term : cityTerm, term, ct);
   return mapper.Map<List<GmachDirectoryEntryDto>>(gmachs);
  });
  return Ok(result);
 }

 [HttpGet("{id:int}"),AllowAnonymous]
 public async Task<IActionResult> Details(int id, CancellationToken ct)
 {
  var result = await cache.GetOrCreateAsync(("public-gmach-details", id), async entry =>
  {
   entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
   var gmach = await service.GetPublicDetailsAsync(id, ct);
   if (gmach == null) entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2);
   return gmach;
  });
  return result == null ? NotFound() : Ok(result);
 }
 [HttpPost("mine"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> CreateMine([FromBody]GmachDto dto,CancellationToken ct){
  if(await service.GetMineAsync(UserId,ct)!=null)return Conflict(new {message="כבר קיים גמ״ח המשויך לחשבון."});
  var g=await service.CreateAsync(new Gmach{Name=dto.Name.Trim(),City=dto.City.Trim(),Description=dto.Description,OwnerId=UserId},ct);
  return Created($"/api/gmachs/{g.Id}",mapper.Map<GmachSummaryDto>(g));
 }
 [HttpGet("mine"), Authorize(Roles = "GmachAdmin")]
 public async Task<IActionResult> Mine(CancellationToken ct)
 {
  var gmach = await service.GetMineAsync(UserId, ct);
  if (gmach == null) return NotFound(new { message = "לא נמצא גמ״ח משויך לחשבון." });

  var details = await service.GetAsync(gmach.Id, ct) ?? gmach;
  return Ok(mapper.Map<OwnerGmachDto>(details));
 }
 [HttpPost("mine/items"),Authorize(Roles="GmachAdmin")] public async Task<IActionResult> AddItem([FromBody]CreateItemDto dto,CancellationToken ct){var g=await service.GetMineAsync(UserId,ct);if(g==null)return Forbid();var item=await service.AddItemAsync(g.Id,dto.Name.Trim(),dto.Description?.Trim() ?? string.Empty,ct);return Created($"/api/items/{item.Id}",mapper.Map<ItemSummaryDto>(item));}
 public sealed class MarkBorrowedRequest {
  [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)] public string BorrowerName {get;set;} = "";
  [System.ComponentModel.DataAnnotations.EmailAddress] public string? BorrowerEmail {get;set;}
  [System.ComponentModel.DataAnnotations.Phone] public string? BorrowerPhone {get;set;}
  public DateTime? ExpectedReturnDate {get;set;}
 }
 [HttpPost("mine/items/{itemId:int}/loans"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> MarkBorrowed(int itemId,[FromBody] MarkBorrowedRequest request,CancellationToken ct){
  if(string.IsNullOrWhiteSpace(request.BorrowerName))return BadRequest(new {message="חובה למלא שם שואל."});
  if(string.IsNullOrWhiteSpace(request.BorrowerEmail)&&string.IsNullOrWhiteSpace(request.BorrowerPhone))return BadRequest(new {message="חובה למלא לפחות טלפון או אימייל."});
  var loan=await service.MarkBorrowedAsync(UserId,itemId,request.BorrowerName.Trim(),string.IsNullOrWhiteSpace(request.BorrowerPhone)?null:request.BorrowerPhone.Trim(),string.IsNullOrWhiteSpace(request.BorrowerEmail)?null:request.BorrowerEmail.Trim(),ct,request.ExpectedReturnDate);
  if (loan != null) return Ok(mapper.Map<ManagedLoanDto>(loan));
  logger.LogWarning("No copy available for a managed loan on item {ItemId}", itemId);
  return Conflict(new { message = "אין עותק זמין להשאלה." });
 }
 [HttpGet("mine/managed-loans"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> ManagedLoans(CancellationToken ct){
  var loans=await service.GetManagedLoansAsync(UserId,ct);
  var ownedGmach=await service.GetMineAsync(UserId,ct);
  var gmach=ownedGmach==null?null:await service.GetAsync(ownedGmach.Id,ct);
  var copies=(gmach?.Items??Enumerable.Empty<GmachManagement.Core.Entities.Item>())
   .SelectMany(item=>item.Copies.Select(copy=>new {copy.Id,ItemId=item.Id,ItemName=item.Name}))
   .ToDictionary(copy=>copy.Id);
  return Ok(loans.Select(loan =>
  {
   var dto = mapper.Map<ManagedLoanDetailsDto>(loan);
   return copies.TryGetValue(loan.ItemCopyId, out var copy)
    ? dto with { ItemId = copy.ItemId, ItemName = copy.ItemName }
    : dto;
  }));
 }
 [HttpPut("mine/managed-loans/{id:int}/due-date"), Authorize(Roles = "GmachAdmin")]
 public async Task<IActionResult> ChangeManagedLoanDueDate(
  [FromRoute] int id, [FromBody] ManagedLoanDueDateRequest request, CancellationToken ct)
 {
  try
  {
   var updated = await service.SetManagedLoanDueDateAsync(UserId, id, request.ExpectedReturnDate, ct);
   return updated ? NoContent() : NotFound();
  }
  catch (ArgumentOutOfRangeException)
  {
   return BadRequest(new { message = "מועד ההחזרה חייב להיות בעתיד." });
  }
 }

 public sealed record ManagedLoanDueDateRequest(DateTime ExpectedReturnDate);

 [HttpPut("mine/managed-loans/{id:int}/returns/current"),Authorize(Roles="GmachAdmin")]
 public async Task<IActionResult> ReturnManagedLoan(int id,CancellationToken ct)=>await service.ReturnManagedLoanAsync(UserId,id,ct)?NoContent():NotFound();
}
