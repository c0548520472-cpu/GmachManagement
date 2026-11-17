using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services;

public sealed class GmachCommunicationService(IGmachCommunicationRepository repository)
{
    public Task<List<BorrowApproval>> GetApprovalsAsync(int userId, CancellationToken ct)
        => repository.GetApprovalsAsync(userId, ct);

    public Task MarkApprovalSeenAsync(int userId, int requestId, CancellationToken ct)
        => repository.MarkApprovalSeenAsync(userId, requestId, ct);

    public Task<List<GmachChatThread>> GetOwnerInboxAsync(int ownerId, CancellationToken ct)
        => repository.GetOwnerInboxAsync(ownerId, ct);

    public Task<List<GmachChatMessage>?> GetChatAsync(int gmachId, int? customerId, int actorId, CancellationToken ct)
        => repository.GetChatAsync(gmachId, customerId, actorId, ct);

    public Task MarkChatReadByOwnerAsync(int gmachId, int customerId, int ownerId, int throughId, CancellationToken ct)
        => repository.MarkChatReadByOwnerAsync(gmachId, customerId, ownerId, throughId, ct);

    public Task<ChatAttachment?> GetAttachmentAsync(int gmachId, int messageId, int actorId, CancellationToken ct)
        => repository.GetAttachmentAsync(gmachId, messageId, actorId, ct);

    public Task<(bool Saved, string? OwnerEmail)> AddMessageAsync(
        int gmachId, int? customerId, int actorId, string message, int? itemId, ChatAttachment? attachment, CancellationToken ct)
        => repository.AddMessageAsync(gmachId, customerId, actorId, message.Trim(), itemId, attachment, ct);
}
