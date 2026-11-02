using AutoMapper;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GmachManagement.Service.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();

            CreateMap<Item, ItemDto>();

            CreateMap<Loan, LoanDto>();
        }
    }
}

