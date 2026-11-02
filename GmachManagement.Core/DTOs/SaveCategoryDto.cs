using System;
using System.Collections.Generic;
using System.Text;


using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs;

public class SaveCategoryDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

