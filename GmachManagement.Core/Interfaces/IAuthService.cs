using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.DTOs;

namespace GmachManagement.Core.Interfaces
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(
            LoginDto request,
            CancellationToken cancellationToken = default);


        Task<string?> RegisterWithRecoveryAsync(RegisterDto request,CancellationToken cancellationToken=default);
        Task<string?> GenerateRecoveryCodeAsync(int userId,CancellationToken cancellationToken=default);
        Task<bool> ResetWithRecoveryAsync(string email,string code,string newPassword,CancellationToken cancellationToken=default);
        Task<bool> RegisterAsync(
            RegisterDto request,
            CancellationToken cancellationToken = default);

    }
}
    

