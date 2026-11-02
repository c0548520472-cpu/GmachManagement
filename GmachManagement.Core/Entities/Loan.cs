using System;
using System.Collections.Generic;
using System.Text;


namespace GmachManagement.Core.Entities
{
    public class Loan
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ItemCopyId { get; set; }
        public ItemCopy ItemCopy { get; set; } = null!;

        public DateTime LoanDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
    }
}

