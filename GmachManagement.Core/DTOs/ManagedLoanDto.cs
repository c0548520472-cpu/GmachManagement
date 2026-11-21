namespace GmachManagement.Core.DTOs;

public sealed class ManagedLoanDto
{
    public int Id { get; init; }
    public int GmachId { get; init; }
    public int ItemCopyId { get; init; }
    public string BorrowerName { get; init; } = string.Empty;
    public string? BorrowerPhone { get; init; }
    public string? BorrowerEmail { get; init; }
    public DateTime BorrowedAt { get; init; }
    public DateTime? ExpectedReturnDate { get; init; }
    public DateTime? ReturnedAt { get; init; }
}
