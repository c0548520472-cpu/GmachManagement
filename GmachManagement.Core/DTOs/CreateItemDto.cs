using System;
using System.Collections.Generic;
using System.Text;


using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs;

public class CreateItemDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
}

