using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs;

public class CreateBorrowRequestDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ItemCopyId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string ApplicantName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Phone) && string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("יש להזין טלפון או כתובת אימייל", new[] { nameof(Phone), nameof(Email) });
    }
}
