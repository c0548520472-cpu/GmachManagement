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
 public async Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct){var item=new Item{Name=name,Description=description,GmachId=gmachId};db.Items.Add(item);await db.SaveChangesAsync(ct);return item;}
 public Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct)=>db.Loans.AnyAsync(l=>l.Id==loanId && l.ItemCopy.Item.Gmach!=null && l.ItemCopy.Item.Gmach.OwnerId==ownerId,ct);
 public Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct)=>db.Items.AnyAsync(i=>i.Id==itemId && i.Gmach!=null && i.Gmach.OwnerId==ownerId,ct);
}
