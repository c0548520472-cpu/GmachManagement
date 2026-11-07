using GmachManagement.Core.Entities;
namespace GmachManagement.Core.Interfaces;
public interface IGmachRepository {
 Task<List<Gmach>> SearchAsync(string? city,string? search,CancellationToken ct);
 Task<Gmach?> GetAsync(int id,CancellationToken ct);
 Task<Gmach?> GetByOwnerAsync(int ownerId,CancellationToken ct);
 Task<Gmach> CreateAsync(Gmach gmach,CancellationToken ct);
 Task<int> ImportItemsAsync(int gmachId,IReadOnlyList<(string Name,string Description,int Quantity)> rows,CancellationToken ct);
 Task<Item> AddItemAsync(int gmachId,string name,string description,CancellationToken ct);
 Task<GmachManagement.Core.Entities.ManagedLoan?> MarkBorrowedAsync(int ownerId,int itemId,string name,string? phone,string? email,CancellationToken ct);
 Task<bool> ReturnManagedLoanAsync(int ownerId,int id,CancellationToken ct);
 Task<List<GmachManagement.Core.Entities.ManagedLoan>> GetManagedLoansAsync(int ownerId,CancellationToken ct);
 Task<int> AddCopiesAsync(int itemId,int quantity,CancellationToken ct);
 Task<int?> GetItemIdForCopyAsync(int copyId,CancellationToken ct);
 Task<bool> OwnsLoanAsync(int ownerId,int loanId,CancellationToken ct);
 Task<bool> OwnsItemAsync(int ownerId,int itemId,CancellationToken ct);
}
