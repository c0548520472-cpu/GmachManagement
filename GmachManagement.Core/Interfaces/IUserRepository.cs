using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> RequiresReRegistrationApprovalAsync(string email,CancellationToken ct=default);
        Task<bool> RequestReRegistrationAsync(string email,CancellationToken ct=default);
        Task<int> GetGoogleUserIdAsync(string subject, CancellationToken ct);
        Task SaveGoogleIdentityAsync(User user, string subject, CancellationToken ct);
        Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
    
    
        Task<(User? User, string? Error)> UpdateProfileAsync(int userId, string name, string email, string? passwordHash, bool isOwner, string? gmachName, string? city, string? gmachType, CancellationToken cancellationToken = default);

        Task<GmachManagement.Core.Entities.Gmach?> GetOwnedGmachAsync(int userId,CancellationToken cancellationToken=default);
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> SetRecoveryCodeHashAsync(int userId, string codeHash, CancellationToken cancellationToken = default);
        Task<bool> ConsumeRecoveryCodeAsync(string email, string expectedCodeHash, string newPasswordHash, CancellationToken cancellationToken = default);
        Task<bool> UpdatePasswordHashAsync(int userId, string expectedHash, string newHash, CancellationToken cancellationToken = default);

        Task<User> AddWithGmachAsync(User user, Gmach gmach, CancellationToken cancellationToken = default);

        Task<User> AddAsync(
            User user,
            CancellationToken cancellationToken = default);

    }
 } 

