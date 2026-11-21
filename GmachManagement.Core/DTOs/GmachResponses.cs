namespace GmachManagement.Core.DTOs;

public sealed record GmachSummaryDto(int Id, string Name, string City, string? Description);

public sealed record ItemSummaryDto(int Id, string Name, string? Description);

public sealed record OwnerGmachDto(int Id, string Name, string City, string? Description, IReadOnlyList<OwnerGmachItemDto> Items);

public sealed record OwnerGmachItemDto(
    int Id, string Name, string? Description, bool IsActive, int TotalCopies, int AvailableCopies);

public sealed record ManagedLoanDetailsDto : ManagedLoanDto
{
    public int? ItemId { get; init; }
    public string? ItemName { get; init; }
}
