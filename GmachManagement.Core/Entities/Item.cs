using System;
using System.Collections.Generic;
using System.Text;


namespace GmachManagement.Core.Entities
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;


        public ICollection<ItemCopy> Copies { get; set; }
            = new List<ItemCopy>();

        public ICollection<Category> Categories { get; set; }
            = new List<Category>();
    }
}

