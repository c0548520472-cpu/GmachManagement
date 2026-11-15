namespace GmachManagement.Core.Entities;

public class BorrowRequest
{
    public int Id { get; set; }
    public int ItemCopyId { get; set; }
    public ItemCopy ItemCopy { get; set; } = null!;
    public string ApplicantName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }
}
