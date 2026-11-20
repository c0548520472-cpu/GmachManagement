using System;
using System.Collections.Generic;
using System.Text;


using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs;

public class RegisterDto : IValidatableObject
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsGmachOwner { get; set; }
    [StringLength(150)] public string? GmachName { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? GmachType { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "הסיסמה חייבת לכלול לפחות 8 תווים.")]
    [MaxLength(128, ErrorMessage = "הסיסמה ארוכה מדי.")]
    [RegularExpression(@"(?s)\A(?=.*\p{L})(?=.*\p{Nd}).*\z",
        ErrorMessage = "הסיסמה חייבת לכלול לפחות אות אחת וספרה אחת.")]
    public string Password { get; set; } = string.Empty;

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012345", "1234567890123456", "0123456789012345",
        "abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnop", "qwertyuiopasdfgh",
        "password12345678", "password123456789", "passwordpassword",
        "1234567890qwerty", "1234567890abcdef"
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsGmachOwner && string.IsNullOrWhiteSpace(GmachName))
            yield return new ValidationResult("יש להזין שם גמ״ח.", new[] { nameof(GmachName) });
        if (IsGmachOwner && string.IsNullOrWhiteSpace(City))
            yield return new ValidationResult("יש להזין עיר.", new[] { nameof(City) });
        if (CommonPasswords.Contains(Password) ||
            System.Text.RegularExpressions.Regex.IsMatch(Password, @"\A(.{1,4})\1+\z",
                System.Text.RegularExpressions.RegexOptions.Singleline))
        {
            yield return new ValidationResult("הסיסמה קלה לניחוש. בחר סיסמה אחרת או השתמש בהצעה של מנהל הסיסמאות.",
                new[] { nameof(Password) });
        }
    }
}
