using System;
using System.Collections.Generic;
using System.Text;


namespace GmachManagement.Core.Exceptions
{
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException()
            : base("העותק נתפס על ידי משתמש אחר, נסה שנית")
        {
        }
    }
}

