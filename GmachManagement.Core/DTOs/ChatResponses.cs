namespace GmachManagement.Core.DTOs;

public sealed record ChatMessageDto(int Id, string Text, DateTime SentAt, string? AttachmentName, bool ReadByOwner)
{
    public bool Mine { get; init; }
    public bool ReadByRecipient { get; init; }
}

public sealed record ChatMessagesDto(IReadOnlyList<ChatMessageDto> Messages, bool CounterpartTyping);
