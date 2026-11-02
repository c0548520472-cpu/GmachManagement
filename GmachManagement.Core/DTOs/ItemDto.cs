using System;
using System.Collections.Generic;
using System.Text;



namespace GmachManagement.Core.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    public bool IsActive { get; set; }

}

