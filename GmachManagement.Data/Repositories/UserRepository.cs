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
                    user => user.Email.ToLower() == email.Trim().ToLower(),
                    cancellationToken);
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public async Task<bool> UpdatePasswordHashAsync(int userId, string expectedHash, string newHash, CancellationToken cancellationToken = default)
        {
            return await _context.Users.Where(u => u.Id == userId && u.PasswordHash == expectedHash)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, newHash), cancellationToken) == 1;
        }

        public async Task<bool> SetRecoveryCodeHashAsync(int userId,string codeHash,CancellationToken ct=default)
        {
            return await _context.Users.Where(u=>u.Id==userId).ExecuteUpdateAsync(setters=>setters.SetProperty(u=>u.RecoveryCodeHash,codeHash),ct)==1;
        }
        public async Task<bool> ConsumeRecoveryCodeAsync(string email,string expectedCodeHash,string newPasswordHash,CancellationToken ct=default)
        {
            return await _context.Users.Where(u=>u.Email==email && u.RecoveryCodeHash==expectedCodeHash)
              .ExecuteUpdateAsync(setters=>setters.SetProperty(u=>u.PasswordHash,newPasswordHash).SetProperty(u=>u.RecoveryCodeHash,(string?)null),ct)==1;
        }
        public async Task<User> AddWithGmachAsync(User user, Gmach gmach, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            gmach.OwnerId = user.Id;
            _context.Gmachs.Add(gmach);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
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

