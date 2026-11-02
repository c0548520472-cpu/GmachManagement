
using System;
using System.ComponentModel.DataAnnotations;

namespace GmachManagement.Core.DTOs
{
    public class BorrowItemDto
    {
        [Range(1, int.MaxValue)]
        public int ItemCopyId { get; set; }

        [Required]
        public DateTime ExpectedReturnDate { get; set; }
    }
}
