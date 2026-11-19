namespace GmachManagement.Core.Interfaces;

public sealed record GmachChatMessage(int Id, int SenderId, string Text, DateTime SentAt, DateTime? ReadByOwnerAt, DateTime? ReadByCustomerAt, string? AttachmentName);
public sealed record ChatAttachment(string Name, byte[] Content);
public sealed record GmachChatThread(int GmachId, int CustomerId, string CustomerName, int UnreadCount, DateTime LastMessageAt, string LastMessagePreview);
public sealed record ChatUnreadNotice(int GmachId, int? CustomerId, string GmachName, int UnreadCount);
public sealed record BorrowApproval(int Id, string ItemName, string GmachName, DateTime ApprovedAt, bool Seen);

public interface IGmachCommunicationRepository
{
    Task<List<ChatUnreadNotice>> GetUnreadChatsAsync(int userId, CancellationToken ct);
    Task<List<BorrowApproval>> GetApprovalsAsync(int userId, CancellationToken ct);
    Task MarkApprovalSeenAsync(int userId, int requestId, CancellationToken ct);
    Task<List<GmachChatThread>> GetOwnerInboxAsync(int ownerId, CancellationToken ct);
    Task<List<GmachChatMessage>?> GetChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct);
    Task<bool> CanAccessChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct);
    Task MarkChatReadByCustomerAsync(int gmachId, int customerId, int throughId, CancellationToken ct);
    Task MarkChatReadByOwnerAsync(int gmachId, int customerId, int ownerId, int throughId, CancellationToken ct);
    Task<bool> EditOwnMessageAsync(int gmachId, int? customerId, int actorId, int messageId, string text, CancellationToken ct);
    Task<bool> DeleteOwnMessageAsync(int gmachId, int? customerId, int actorId, int messageId, CancellationToken ct);
    Task<ChatAttachment?> GetAttachmentAsync(int gmachId, int messageId, int actorId, CancellationToken ct);
    Task<(bool Saved, string? NotificationEmail, bool ToCustomer)> AddMessageAsync(
        int gmachId, int? customerId, int actorId, string message, int? itemId, ChatAttachment? attachment, CancellationToken ct);
}
