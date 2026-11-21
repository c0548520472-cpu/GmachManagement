using AutoMapper;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
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

            CreateMap<Item, ItemDto>()
                .ForMember(d => d.TotalCopies, o => o.MapFrom(s => s.Copies.Count))
                .ForMember(d => d.AvailableCopies, o => o.MapFrom(s => s.IsActive
                    ? s.Copies.Count(c => c.Status == ItemStatus.Available) : 0));
            CreateMap<ItemCopy, ItemCopyDto>();
            CreateMap<Category, CategoryDto>();

            CreateMap<BorrowRequest, BorrowRequestDto>()
                .ForCtorParam(nameof(BorrowRequestDto.ItemName), option => option.MapFrom(request => request.Item.Name));
            CreateMap<Gmach, GmachDirectoryEntryDto>();
            CreateMap<Gmach, GmachSummaryDto>();
            CreateMap<Gmach, OwnerGmachDto>();
            CreateMap<Item, ItemSummaryDto>();
            CreateMap<Item, OwnerGmachItemDto>()
                .ForCtorParam(nameof(OwnerGmachItemDto.TotalCopies), option => option.MapFrom(item => item.Copies.Count))
                .ForCtorParam(nameof(OwnerGmachItemDto.AvailableCopies), option =>
                    option.MapFrom(item => item.Copies.Count(copy => copy.Status == ItemStatus.Available)));
            CreateMap<Loan, LoanDto>();
            CreateMap<PersonalTransaction, PersonalTransactionDto>();
            CreateMap<ManagedLoan, ManagedLoanDto>();
            CreateMap<ManagedLoan, ManagedLoanDetailsDto>()
                .IncludeBase<ManagedLoan, ManagedLoanDto>()
                .ForMember(loan => loan.ItemId, option => option.Ignore())
                .ForMember(loan => loan.ItemName, option => option.Ignore());
        }
    }
}

