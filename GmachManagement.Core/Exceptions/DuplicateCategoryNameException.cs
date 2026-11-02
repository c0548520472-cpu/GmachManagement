using System;
using System.Collections.Generic;
using System.Text;


namespace GmachManagement.Core.Exceptions;

public class DuplicateCategoryNameException : Exception
{
    public DuplicateCategoryNameException()
        : base("A category with this name already exists.")
    {
    }
}

