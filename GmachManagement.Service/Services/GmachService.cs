using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
namespace GmachManagement.Service.Services;
public sealed class GmachService {
 public Task<string?> GetOwnerEmailForItemAsync(int itemId,CancellationToken ct)=>repo.GetOwnerEmailForItemAsync(itemId,ct);
 private readonly IGmachRepository repo;
 public GmachService(IGmachRepository repo)=>this.repo=repo;
 public Task<BorrowRequest?> AddBorrowRequestAsync(int itemId,string name,string? phone,string? email,CancellationToken ct,int? applicantId = null)=>repo.AddBorrowRequestAsync(itemId,name,phone,email,ct,applicantId);
 public Task<List<BorrowRequest>> GetBorrowRequestsAsync(int ownerId,CancellationToken ct)=>repo.GetBorrowRequestsAsync(ownerId,ct);
 public Task<bool> FulfillBorrowRequestAsync(int ownerId,int requestId,CancellationToken ct)=>repo.FulfillBorrowRequestAsync(ownerId,requestId,ct);
 public Task<List<Gmach>> SearchAsync(string? city,string? search,CancellationToken ct)=>repo.SearchAsync(city,search,ct);
 public Task<List<Gmach>> SearchAsync(string? city,string? search,string? itemSearch,CancellationToken ct)=>repo.SearchAsync(city,search,itemSearch,ct);
 public Task<List<Gmach>> SearchDirectoryAsync(string city, string term, CancellationToken ct)
     => repo.SearchDirectoryAsync(city, term, ct);
 public Task<PublicGmachDetailsDto?> GetPublicDetailsAsync(int id, CancellationToken ct)
     => repo.GetPublicDetailsAsync(id, ct);
 public Task<Gmach?> GetAsync(int id,CancellationToken ct)=>repo.GetAsync(id,ct);
 public Task<Gmach> CreateAsync(Gmach g,CancellationToken ct)=>repo.CreateAsync(g,ct);
 public Task<Gmach?> GetMineAsync(int ownerId,CancellationToken ct)=>repo.GetByOwnerAsync(ownerId,ct);
 public Task<int> ImportItemsAsync(int gmachId,IReadOnlyList<(string Name,string Description,int Quantity)> rows,CancellationToken ct)=>repo.ImportItemsAsync(gmachId,rows,ct);
 public Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct)=>repo.AddItemAsync(gmachId,name,description,ct);
 public Task<int?> GetItemIdForCopyAsync(int copyId,CancellationToken ct)=>repo.GetItemIdForCopyAsync(copyId,ct);
 public Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct)=>repo.OwnsLoanAsync(ownerId,loanId,ct);
 public Task<GmachManagement.Core.Entities.ManagedLoan?> MarkBorrowedAsync(int ownerId,int itemId,string name,string? phone,string? email,CancellationToken ct,DateTime? expectedReturnDate=null)=>repo.MarkBorrowedAsync(ownerId,itemId,name,phone,email,ct,expectedReturnDate);
 public Task<bool> SetManagedLoanDueDateAsync(int ownerId,int id,DateTime dueDate,CancellationToken ct)=>repo.SetManagedLoanDueDateAsync(ownerId,id,dueDate,ct);
 public Task<bool> ReturnManagedLoanAsync(int ownerId,int id,CancellationToken ct)=>repo.ReturnManagedLoanAsync(ownerId,id,ct);
 public Task<List<GmachManagement.Core.Entities.ManagedLoan>> GetManagedLoansAsync(int ownerId,CancellationToken ct)=>repo.GetManagedLoansAsync(ownerId,ct);
 public Task<int> AddCopiesAsync(int itemId,int quantity,CancellationToken ct)=>repo.AddCopiesAsync(itemId,quantity,ct);
 public Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct)=>repo.OwnsItemAsync(ownerId,itemId,ct);
}
