namespace GmachManagement.Core.DTOs;

public class BorrowRequestDto
{
    public int Id { get; set; }
    public int ItemCopyId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}
