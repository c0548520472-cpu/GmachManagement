using System;
using System.Collections.Generic;
using System.Text;


using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs;

public class RegisterDto
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

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
