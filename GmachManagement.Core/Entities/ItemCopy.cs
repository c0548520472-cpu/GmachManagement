using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;
using GmachManagement.Core.Enums;

namespace GmachManagement.Core.Entities
{
    public class ItemCopy
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public ItemStatus Status { get; set; } = ItemStatus.Available;
           

        [Timestamp]
        public byte[] RowVersion { get; set; }= Array.Empty<byte>();
            

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
           
    }
}
