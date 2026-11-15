using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces;

public interface IBorrowRequestRepository
{
    Task<BorrowRequest?> CreateAsync(int itemCopyId, string name, string? phone, string? email, CancellationToken ct);
    Task<List<BorrowRequest>> GetPendingForOwnerAsync(int ownerId, CancellationToken ct);
    Task<BorrowRequest?> FulfillAsync(int requestId, int ownerId, CancellationToken ct);
}
