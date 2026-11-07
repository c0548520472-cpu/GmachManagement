using System;
using System.Collections.Generic;
using System.Text;
using GmachManagement.Core.Enums;


namespace GmachManagement.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public Gmach? OwnedGmach { get; set; }

        public ICollection<Loan> Loans { get; set; }= new List<Loan>();
    

        public ICollection<PersonalTransaction> PersonalTransactions { get; set; } = new List<PersonalTransaction>();
           
    }
}

