namespace GmachManagement.Core.DTOs;

public sealed record PublicGmachDetailsDto(int Id, string Name, string City, string? Description, List<PublicGmachItemDto> Items);

public sealed record PublicGmachItemDto(int Id, string Name, string Description, bool IsActive, int AvailableCopies);
