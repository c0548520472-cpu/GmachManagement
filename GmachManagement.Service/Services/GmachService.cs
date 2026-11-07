using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
namespace GmachManagement.Service.Services;
public sealed class GmachService {
 private readonly IGmachRepository repo;
 public GmachService(IGmachRepository repo)=>this.repo=repo;
 public Task<List<Gmach>> SearchAsync(string? city,string? search,CancellationToken ct)=>repo.SearchAsync(city,search,ct);
 public Task<Gmach?> GetAsync(int id,CancellationToken ct)=>repo.GetAsync(id,ct);
 public Task<Gmach> CreateAsync(Gmach g,CancellationToken ct)=>repo.CreateAsync(g,ct);
 public Task<Gmach?> GetMineAsync(int ownerId,CancellationToken ct)=>repo.GetByOwnerAsync(ownerId,ct);
 public Task<int> ImportItemsAsync(int gmachId,IReadOnlyList<(string Name,string Description,int Quantity)> rows,CancellationToken ct)=>repo.ImportItemsAsync(gmachId,rows,ct);
 public Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct)=>repo.AddItemAsync(gmachId,name,description,ct);
 public Task<int?> GetItemIdForCopyAsync(int copyId,CancellationToken ct)=>repo.GetItemIdForCopyAsync(copyId,ct);
 public Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct)=>repo.OwnsLoanAsync(ownerId,loanId,ct);
 public Task<GmachManagement.Core.Entities.ManagedLoan?> MarkBorrowedAsync(int ownerId,int itemId,string name,string? phone,string? email,CancellationToken ct)=>repo.MarkBorrowedAsync(ownerId,itemId,name,phone,email,ct);
 public Task<bool> ReturnManagedLoanAsync(int ownerId,int id,CancellationToken ct)=>repo.ReturnManagedLoanAsync(ownerId,id,ct);
 public Task<List<GmachManagement.Core.Entities.ManagedLoan>> GetManagedLoansAsync(int ownerId,CancellationToken ct)=>repo.GetManagedLoansAsync(ownerId,ct);
 public Task<int> AddCopiesAsync(int itemId,int quantity,CancellationToken ct)=>repo.AddCopiesAsync(itemId,quantity,ct);
 public Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct)=>repo.OwnsItemAsync(ownerId,itemId,ct);
}
