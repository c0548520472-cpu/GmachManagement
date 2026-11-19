using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services;

public sealed class GmachCommunicationService(IGmachCommunicationRepository repository)
{
    public Task<List<ChatUnreadNotice>> GetUnreadChatsAsync(int userId, CancellationToken ct)
        => repository.GetUnreadChatsAsync(userId, ct);

    public Task<List<BorrowApproval>> GetApprovalsAsync(int userId, CancellationToken ct)
        => repository.GetApprovalsAsync(userId, ct);

    public Task MarkApprovalSeenAsync(int userId, int requestId, CancellationToken ct)
        => repository.MarkApprovalSeenAsync(userId, requestId, ct);

    public Task<List<GmachChatThread>> GetOwnerInboxAsync(int ownerId, CancellationToken ct)
        => repository.GetOwnerInboxAsync(ownerId, ct);

    public Task<List<GmachChatMessage>?> GetChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct)
        => repository.GetChatAsync(gmachId, customerId, actorId, ct);

    public Task<bool> CanAccessChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct)
        => repository.CanAccessChatAsync(gmachId, customerId, actorId, ct);

    public Task MarkChatReadByCustomerAsync(int gmachId, int customerId, int throughId, CancellationToken ct)
        => repository.MarkChatReadByCustomerAsync(gmachId, customerId, throughId, ct);

    public Task MarkChatReadByOwnerAsync(int gmachId, int customerId, int ownerId, int throughId, CancellationToken ct)
        => repository.MarkChatReadByOwnerAsync(gmachId, customerId, ownerId, throughId, ct);

    public Task<bool> EditOwnMessageAsync(int gmachId, int? customerId, int actorId, int messageId, string text, CancellationToken ct)
        => repository.EditOwnMessageAsync(gmachId, customerId, actorId, messageId, text, ct);

    public Task<bool> DeleteOwnMessageAsync(int gmachId, int? customerId, int actorId, int messageId, CancellationToken ct)
        => repository.DeleteOwnMessageAsync(gmachId, customerId, actorId, messageId, ct);

    public Task<ChatAttachment?> GetAttachmentAsync(int gmachId, int messageId, int actorId, CancellationToken ct)
        => repository.GetAttachmentAsync(gmachId, messageId, actorId, ct);

    public Task<(bool Saved, string? NotificationEmail, bool ToCustomer)> AddMessageAsync(
        int gmachId, int? customerId, int actorId, string message, int? itemId, ChatAttachment? attachment, CancellationToken ct)
        => repository.AddMessageAsync(gmachId, customerId, actorId, message.Trim(), itemId, attachment, ct);
}
