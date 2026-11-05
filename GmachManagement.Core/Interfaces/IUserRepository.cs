using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
    
    
        Task<bool> UpdatePasswordHashAsync(int userId, string expectedHash, string newHash, CancellationToken cancellationToken = default);

        Task<User> AddAsync(
            User user,
            CancellationToken cancellationToken = default);

    }
 } 

