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

        public async Task<(User? User, string? Error)> UpdateProfileAsync(int userId,string name,string email,string? passwordHash,bool isOwner,string? gmachName,string? city,string? gmachType,CancellationToken ct=default)
        {
            await using var transaction=await _context.Database.BeginTransactionAsync(ct);
            var user=await _context.Users.FirstOrDefaultAsync(u=>u.Id==userId,ct);
            if(user==null)return (null,"המשתמש לא נמצא.");
            if(await _context.Users.AnyAsync(u=>u.Id!=userId&&u.Email.ToLower()==email.ToLower(),ct))return (null,"כתובת המייל כבר רשומה במערכת.");
            var gmach=await _context.Gmachs.FirstOrDefaultAsync(g=>g.OwnerId==userId,ct);
            if(!isOwner&&gmach!=null){
                if(await _context.Items.AnyAsync(i=>i.GmachId==gmach.Id,ct) || await _context.ManagedLoans.AnyAsync(l=>l.GmachId==gmach.Id,ct))
                    return (null,"לא ניתן לבטל בעלות כל עוד יש לגמ״ח פריטים או השאלות. הנתונים יישמרו ללא שינוי.");
                _context.Gmachs.Remove(gmach);
            }
            user.Name=name;
            user.Email=email;
            if(passwordHash!=null)user.PasswordHash=passwordHash;
            user.Role=isOwner?GmachManagement.Core.Enums.UserRole.GmachAdmin:GmachManagement.Core.Enums.UserRole.User;
            if(isOwner){
                if(gmach==null){gmach=new Gmach{OwnerId=userId,Name=gmachName!,City=city!,Type=gmachType??""};_context.Gmachs.Add(gmach);}
                else{gmach.Name=gmachName!;gmach.City=city!;gmach.Type=gmachType??"";}
            }
            try{await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);return (user,null);}
            catch(DbUpdateException ex) when(ex.InnerException?.GetType().Name=="PostgresException" || ex.InnerException is Microsoft.Data.SqlClient.SqlException){return (null,"לא ניתן לשמור את הפרטים. ייתכן שכתובת המייל כבר רשומה.");}
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

        public Task<Gmach?> GetOwnedGmachAsync(int userId,CancellationToken ct=default)=>_context.Gmachs.AsNoTracking().FirstOrDefaultAsync(g=>g.OwnerId==userId,ct);
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

