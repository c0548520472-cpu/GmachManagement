using System;
using System.Collections.Generic;
using System.Text;


using System.ComponentModel.DataAnnotations;
using GmachManagement.Core.Enums;

namespace GmachManagement.Core.DTOs;

public class UpdateItemCopyStatusDto
{
    [Required]
    [EnumDataType(typeof(ItemStatus))]
    public ItemStatus Status { get; set; }
}

