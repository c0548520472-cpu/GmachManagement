using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmachManagement.Data.Configurations;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext context,
        string email,
        string password)
    {
        var exists = await context.Users
            .AnyAsync(u => u.Email == email);

        if (exists)
            return;

        var admin = new User
        {
            Name = "מנהל הגמח",
            Email = email,
            Role = UserRole.GmachAdmin
        };

        var hasher = new PasswordHasher<User>();

        admin.PasswordHash = hasher.HashPassword(
            admin,
            password);

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}

