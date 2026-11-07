using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
namespace GmachManagement.Service.Services;
public sealed class GmachService {
 private readonly IGmachRepository repo;
 public GmachService(IGmachRepository repo)=>this.repo=repo;
 public Task<List<Gmach>> SearchAsync(string? city,string? search,CancellationToken ct)=>repo.SearchAsync(city,search,ct);
 public Task<Gmach?> GetAsync(int id,CancellationToken ct)=>repo.GetAsync(id,ct);
 public Task<Gmach?> GetMineAsync(int ownerId,CancellationToken ct)=>repo.GetByOwnerAsync(ownerId,ct);
 public Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct)=>repo.AddItemAsync(gmachId,name,description,ct);
 public Task<int?> GetItemIdForCopyAsync(int copyId,CancellationToken ct)=>repo.GetItemIdForCopyAsync(copyId,ct);
 public Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct)=>repo.OwnsLoanAsync(ownerId,loanId,ct);
 public Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct)=>repo.OwnsItemAsync(ownerId,itemId,ct);
}
