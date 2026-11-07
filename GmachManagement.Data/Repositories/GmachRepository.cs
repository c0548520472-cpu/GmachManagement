using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
namespace GmachManagement.Data.Repositories;
public sealed class GmachRepository : IGmachRepository {
 private readonly AppDbContext db;
 public GmachRepository(AppDbContext db) => this.db=db;
 public Task<List<Gmach>> SearchAsync(string? city,string? search,CancellationToken ct) {
  var q=db.Gmachs.AsNoTracking().AsQueryable();
  if(!string.IsNullOrWhiteSpace(city)) q=q.Where(g=>g.City.Contains(city.Trim()));
  if(!string.IsNullOrWhiteSpace(search)) q=q.Where(g=>g.Name.Contains(search.Trim()));
  return q.OrderBy(g=>g.Name).ToListAsync(ct);
 }
 public Task<Gmach?> GetAsync(int id,CancellationToken ct)=>db.Gmachs.AsNoTracking().Include(g=>g.Items).ThenInclude(i=>i.Copies).FirstOrDefaultAsync(g=>g.Id==id,ct);
 public Task<Gmach?> GetByOwnerAsync(int ownerId,CancellationToken ct)=>db.Gmachs.AsNoTracking().FirstOrDefaultAsync(g=>g.OwnerId==ownerId,ct);
 public async Task<Gmach> CreateAsync(Gmach g,CancellationToken ct){db.Gmachs.Add(g);await db.SaveChangesAsync(ct);return g;}
 private static string NormalizeItemText(string? value)=> (value??string.Empty).Trim().ToUpperInvariant();
 private async Task<int> UpsertItemsAsync(int gmachId,IReadOnlyList<(string Name,string Description,int Quantity)> rows,CancellationToken ct){
  // Match by gmach, name and description; merge duplicates in the same upload as well.
  var existing=await db.Items.Where(i=>i.GmachId==gmachId).ToListAsync(ct);
  var lookup=existing.GroupBy(i=>(NormalizeItemText(i.Name),NormalizeItemText(i.Description)))
   .ToDictionary(g=>g.Key,g=>g.OrderByDescending(i=>i.IsActive).ThenBy(i=>i.Id).First());
  var distinct=new HashSet<Item>();
  foreach(var row in rows){
   var name=row.Name.Trim();var description=row.Description.Trim();
   var key=(NormalizeItemText(name),NormalizeItemText(description));
   if(!lookup.TryGetValue(key,out var item)){
    item=new Item{Name=name,Description=description,GmachId=gmachId};
    db.Items.Add(item);lookup.Add(key,item);
   }
   distinct.Add(item);
   for(var n=0;n<row.Quantity;n++)item.Copies.Add(new ItemCopy{Status=GmachManagement.Core.Enums.ItemStatus.Available});
  }
  await db.SaveChangesAsync(ct);
  return distinct.Count;
 }
 public Task<int> ImportItemsAsync(int gmachId,IReadOnlyList<(string Name,string Description,int Quantity)> rows,CancellationToken ct)
  =>UpsertItemsAsync(gmachId,rows,ct);
 public async Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct){
  var keyName=NormalizeItemText(name);var keyDescription=NormalizeItemText(description);
  var existing=await db.Items.Where(i=>i.GmachId==gmachId).ToListAsync(ct);
  var item=existing.OrderByDescending(i=>i.IsActive).ThenBy(i=>i.Id)
   .FirstOrDefault(i=>NormalizeItemText(i.Name)==keyName && NormalizeItemText(i.Description)==keyDescription);
  if(item==null){item=new Item{Name=name.Trim(),Description=description.Trim(),GmachId=gmachId};db.Items.Add(item);}
  db.ItemCopies.Add(new ItemCopy{Item=item,Status=GmachManagement.Core.Enums.ItemStatus.Available});
  await db.SaveChangesAsync(ct);
  return item;
 }
 public async Task<ManagedLoan?> MarkBorrowedAsync(int ownerId,int itemId,string name,string? phone,string? email,CancellationToken ct){
  var gmach=await db.Gmachs.FirstOrDefaultAsync(g=>g.OwnerId==ownerId,ct);
  if(gmach==null)return null;
  await using var transaction=await db.Database.BeginTransactionAsync(ct);
  var copy=await db.ItemCopies.Include(c=>c.Item).FirstOrDefaultAsync(c=>c.ItemId==itemId && c.Item.GmachId==gmach.Id && c.Item.IsActive && c.Status==GmachManagement.Core.Enums.ItemStatus.Available,ct);
  if(copy==null)return null;
  copy.Status=GmachManagement.Core.Enums.ItemStatus.Borrowed;
  var record=new ManagedLoan{GmachId=gmach.Id,ItemCopyId=copy.Id,BorrowerName=name,BorrowerPhone=phone,BorrowerEmail=email,BorrowedAt=DateTime.UtcNow};
  db.ManagedLoans.Add(record);
  try {await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);return record;}
  catch(DbUpdateConcurrencyException){throw new GmachManagement.Core.Exceptions.ConcurrencyConflictException();}
 }
 public async Task<bool> ReturnManagedLoanAsync(int ownerId,int id,CancellationToken ct){
  var gmach=await db.Gmachs.FirstOrDefaultAsync(g=>g.OwnerId==ownerId,ct);
  if(gmach==null)return false;
  await using var transaction=await db.Database.BeginTransactionAsync(ct);
  var record=await db.ManagedLoans.FirstOrDefaultAsync(l=>l.Id==id && l.GmachId==gmach.Id && l.ReturnedAt==null,ct);
  if(record==null)return false;
  var copy=await db.ItemCopies.FirstOrDefaultAsync(c=>c.Id==record.ItemCopyId,ct);
  if(copy==null)return false;
  record.ReturnedAt=DateTime.UtcNow;copy.Status=GmachManagement.Core.Enums.ItemStatus.Available;
  try {await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);return true;}
  catch(DbUpdateConcurrencyException){throw new GmachManagement.Core.Exceptions.ConcurrencyConflictException();}
 }
 public async Task<List<ManagedLoan>> GetManagedLoansAsync(int ownerId,CancellationToken ct){
  var gmach=await db.Gmachs.AsNoTracking().FirstOrDefaultAsync(g=>g.OwnerId==ownerId,ct);
  return gmach==null?new List<ManagedLoan>():await db.ManagedLoans.AsNoTracking().Where(l=>l.GmachId==gmach.Id && l.ReturnedAt==null).OrderByDescending(l=>l.BorrowedAt).ToListAsync(ct);
 }
 public async Task<int> AddCopiesAsync(int itemId,int quantity,CancellationToken ct){
  if(!await db.Items.AnyAsync(i=>i.Id==itemId && i.IsActive,ct))return 0;
  db.ItemCopies.AddRange(Enumerable.Range(0,quantity).Select(_=>new ItemCopy{ItemId=itemId,Status=GmachManagement.Core.Enums.ItemStatus.Available}));
  await db.SaveChangesAsync(ct);
  return quantity;
 }
 public Task<int?> GetItemIdForCopyAsync(int copyId,CancellationToken ct)=>db.ItemCopies.Where(c=>c.Id==copyId).Select(c=>(int?)c.ItemId).FirstOrDefaultAsync(ct);
 public Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct)=>db.Loans.AnyAsync(l=>l.Id==loanId && l.ItemCopy.Item.Gmach!=null && l.ItemCopy.Item.Gmach.OwnerId==ownerId,ct);
 public Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct)=>db.Items.AnyAsync(i=>i.Id==itemId && i.Gmach!=null && i.Gmach.OwnerId==ownerId,ct);
}
