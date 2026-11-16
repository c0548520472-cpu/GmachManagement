namespace GmachManagement.Core.Interfaces;

public sealed record GmachChatMessage(int Id, int SenderId, string Text, DateTime SentAt, DateTime? ReadByOwnerAt);
public sealed record GmachChatThread(int GmachId, int CustomerId, string CustomerName);
public sealed record BorrowApproval(int Id, string ItemName, string GmachName, DateTime ApprovedAt, bool Seen);

public interface IGmachCommunicationRepository
{
    Task<List<BorrowApproval>> GetApprovalsAsync(int userId, CancellationToken ct);
    Task MarkApprovalSeenAsync(int userId, int requestId, CancellationToken ct);
    Task<List<GmachChatThread>> GetOwnerInboxAsync(int ownerId, CancellationToken ct);
    Task<List<GmachChatMessage>?> GetChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct);
    Task MarkChatReadByOwnerAsync(int gmachId, int customerId, int ownerId, int throughId, CancellationToken ct);
    Task<(bool Saved, string? OwnerEmail)> AddMessageAsync(
        int gmachId, int? customerId, int actorId, string message, int? itemId, CancellationToken ct);
}
