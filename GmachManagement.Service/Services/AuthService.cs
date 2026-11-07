
    using GmachManagement.Core.DTOs;
    using GmachManagement.Core.Interfaces;
    using GmachManagement.Service.Security;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using GmachManagement.Core.Entities;
    using GmachManagement.Core.Enums;

namespace GmachManagement.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            PasswordService passwordService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _configuration = configuration;
        }

        public async Task<(string? Name,string? Email,bool IsOwner,string? GmachName,string? City)> GetProfileAsync(int userId,CancellationToken ct=default)
        {
            var user=await _userRepository.GetByIdAsync(userId,ct);
            if(user==null)return (null,null,false,null,null);
            var gmach=await _userRepository.GetOwnedGmachAsync(userId,ct);
            return (user.Name,user.Email,gmach!=null||user.Role==UserRole.GmachAdmin,gmach?.Name,gmach?.City);
        }
        public async Task<(string? Token,string? Error)> UpdateProfileAsync(int userId,string name,string email,string currentPassword,string? newPassword,bool isOwner,string? gmachName,string? city,CancellationToken ct=default)
        {
            var user=await _userRepository.GetByIdAsync(userId,ct);
            if(user==null||!_passwordService.VerifyPassword(user,currentPassword))return (null,"הסיסמה הנוכחית שגויה.");
            if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(email))return (null,"יש להזין שם וכתובת מייל.");
            if(isOwner&&(string.IsNullOrWhiteSpace(gmachName)||string.IsNullOrWhiteSpace(city)))return (null,"יש להזין שם גמ״ח ועיר.");
            if(newPassword!=null&&(newPassword.Length<8||newPassword.Length>128))return (null,"הסיסמה החדשה חייבת להכיל 8 עד 128 תווים.");
            var newHash=newPassword==null?null:_passwordService.HashPassword(user,newPassword);
            var result=await _userRepository.UpdateProfileAsync(userId,name.Trim(),email.Trim().ToLowerInvariant(),newHash,isOwner,gmachName?.Trim(),city?.Trim(),ct);
            if(result.Error!=null)return (null,result.Error);
            var token=await LoginAsync(new LoginDto{Email=email.Trim(),Password=newPassword??currentPassword},ct);
            return (token,token==null?"יש להתחבר מחדש.":null);
        }

        public async Task<string?> LoginAsync(
            LoginDto request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(
                request.Email.Trim().ToLowerInvariant(),
                cancellationToken);

            if (user == null ||
                !_passwordService.VerifyPassword(user, request.Password))
            {
                return null;
            }

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is missing.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is missing.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is missing.");

            var claims = new[]
            {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateRecoveryCode() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        private static string HashRecoveryCode(string code) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
        public async Task<string?> RegisterWithRecoveryAsync(RegisterDto request,CancellationToken cancellationToken=default)
        {
            var code=CreateRecoveryCode();
            var success=await RegisterInternalAsync(request,HashRecoveryCode(code),cancellationToken);
            return success?code:null;
        }
        public async Task<string?> GenerateRecoveryCodeAsync(int userId,CancellationToken cancellationToken=default)
        {
            var user=await _userRepository.GetByIdAsync(userId,cancellationToken);
            if(user==null)return null;
            var code=CreateRecoveryCode();
            return await _userRepository.SetRecoveryCodeHashAsync(userId,HashRecoveryCode(code),cancellationToken)?code:null;
        }
        public async Task<bool> ResetWithRecoveryAsync(string email,string code,string newPassword,CancellationToken cancellationToken=default)
        {
            if(string.IsNullOrWhiteSpace(email)||string.IsNullOrWhiteSpace(code)||newPassword.Length<8 || newPassword.Length>128)return false;
            var normalizedEmail=email.Trim().ToLowerInvariant();
            var user=await _userRepository.GetByEmailAsync(normalizedEmail,cancellationToken);
            if(user==null)return false;
            var passwordHash=_passwordService.HashPassword(user,newPassword);
            return await _userRepository.ConsumeRecoveryCodeAsync(normalizedEmail,HashRecoveryCode(code.Trim().ToUpperInvariant()),passwordHash,cancellationToken);
        }
        public Task<bool> RegisterAsync(RegisterDto request,CancellationToken cancellationToken=default)=>RegisterInternalAsync(request,null,cancellationToken);
        private Task<bool> RegisterInternalAsync(RegisterDto request,string? recoveryHash,CancellationToken cancellationToken)
            => RegisterImplAsync(request,recoveryHash,cancellationToken);
        private async Task<bool> RegisterImplAsync(
            RegisterDto request,
            string? recoveryHash,
            CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetByEmailAsync(
                request.Email,
                cancellationToken);

            if (existingUser != null)
            {
                return false;
            }


            if (request.IsGmachOwner && (string.IsNullOrWhiteSpace(request.GmachName) || string.IsNullOrWhiteSpace(request.City)))
                throw new ArgumentException("Gmach name and city are required for owners.");

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Role = request.IsGmachOwner ? UserRole.GmachAdmin : UserRole.User,
                RecoveryCodeHash = recoveryHash
            };

            user.PasswordHash = _passwordService.HashPassword(
                user,
                request.Password);

            if (request.IsGmachOwner)
                await _userRepository.AddWithGmachAsync(user, new Gmach { Name = request.GmachName!.Trim(), City = request.City!.Trim() }, cancellationToken);
            else
                await _userRepository.AddAsync(user, cancellationToken);

            return true;

        }

     

   
        
    }
}