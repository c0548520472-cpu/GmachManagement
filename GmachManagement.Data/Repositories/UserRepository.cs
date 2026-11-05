using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.Email == email,
                    cancellationToken);
        }

        public async Task<bool> UpdatePasswordHashAsync(int userId, string expectedHash, string newHash, CancellationToken cancellationToken = default)
        {
            return await _context.Users.Where(u => u.Id == userId && u.PasswordHash == expectedHash)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, newHash), cancellationToken) == 1;
        }

        public async Task<User> AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

    }
}

