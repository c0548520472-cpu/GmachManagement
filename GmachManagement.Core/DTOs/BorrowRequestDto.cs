namespace GmachManagement.Core.DTOs;

public sealed record BorrowRequestDto(
    int Id, int ItemId, string ItemName, string ApplicantName,
    string? Phone, string? Email, DateTime CreatedAt);
