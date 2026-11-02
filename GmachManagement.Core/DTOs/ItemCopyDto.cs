using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Enums;

namespace GmachManagement.Core.DTOs;

public class ItemCopyDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public ItemStatus Status { get; set; }
}

