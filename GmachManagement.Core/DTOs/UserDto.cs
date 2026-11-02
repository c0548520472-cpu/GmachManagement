using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Enums;

namespace GmachManagement.Core.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
