using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.EntityFrameworkCore;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;

namespace GmachManagement.Data.Configurations
{
    public static class SeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "כלי עבודה" },
                new Category { Id = 2, Name = "ציוד רפואי" },
                new Category { Id = 3, Name = "ציוד לאירועים" }
            );

            modelBuilder.Entity<Item>().HasData(
                new Item
                {
                    Id = 1,
                    Name = "מקדחה",
                    Description = "מקדחה חשמלית"
                },
                new Item
                {
                    Id = 2,
                    Name = "כיסא גלגלים",
                    Description = "כיסא גלגלים מתקפל"
                }
            );

            modelBuilder.Entity<ItemCopy>().HasData(
                new
                {
                    Id = 1,
                    ItemId = 1,
                    Status = ItemStatus.Available
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "משתמש בדיקה",
                    Email = "test@example.com",
                    PasswordHash = "TEST_USER_NO_LOGIN",
                    Role = UserRole.User
                }
            );


        }
    }
}
