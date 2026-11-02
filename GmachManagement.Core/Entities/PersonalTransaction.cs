using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Enums;

namespace GmachManagement.Core.Entities
{
    public class PersonalTransaction
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public TransactionType Type { get; set; }

        public string OtherPartyName { get; set; }
            = string.Empty;

        public decimal? Amount { get; set; }

        public string? ItemDescription { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime? ReminderDate { get; set; }

        public bool IsCompleted { get; set; }
    }
}
