using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.DTOs;

namespace GmachManagement.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(string? Name,string? Email,bool IsOwner,string? GmachName,string? City,string? GmachType)> GetProfileAsync(int userId,CancellationToken cancellationToken=default);
        Task<(string? Token,string? Error)> UpdateProfileAsync(int userId,string name,string email,string currentPassword,string? newPassword,bool isOwner,string? gmachName,string? city,string? gmachType,CancellationToken cancellationToken=default);
        Task<string?> LoginAsync(
            LoginDto request,
            CancellationToken cancellationToken = default);


        Task<bool> RegisterAsync(
            RegisterDto request,
            CancellationToken cancellationToken = default);

    }
}
    

