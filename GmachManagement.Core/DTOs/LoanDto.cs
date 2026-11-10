using System;
using System.Collections.Generic;
using System.Text;


namespace GmachManagement.Core.DTOs
{
    public class LoanDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ItemCopyId { get; set; }

        public string? ItemName { get; set; }

        public string? GmachName { get; set; }

        public string? GmachCity { get; set; }

        public string? GmachDescription { get; set; }

        public DateTime LoanDate { get; set; }

        public DateTime ExpectedReturnDate { get; set; }
    }
}
