using System.ComponentModel.DataAnnotations;
using GmachManagement.Core.Enums;

namespace GmachManagement.Core.DTOs;

public class CreatePersonalTransactionDto
{
    [Required, StringLength(150)]
    public string OtherPartyName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ItemDescription { get; set; } = string.Empty;

    public TransactionType Type { get; set; } = TransactionType.Borrowed;

    [Range(0.01, 100000000)]
    public decimal? Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime? ReminderDate { get; set; }
}

public class PersonalTransactionDto
{
    public int Id { get; set; }
    public string OtherPartyName { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public TransactionType Type { get; set; }
    public decimal? Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? ReminderDate { get; set; }
    public bool IsCompleted { get; set; }
}
