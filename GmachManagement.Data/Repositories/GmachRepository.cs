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
