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


        Task<bool> RegisterAsync(
            RegisterDto request,
            CancellationToken cancellationToken = default);

    }
}
    

