
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

        public async Task<(string? Name,string? Email,bool IsOwner,string? GmachName,string? City,string? GmachType)> GetProfileAsync(int userId,CancellationToken ct=default)
        {
            var user=await _userRepository.GetByIdAsync(userId,ct);
            if(user==null)return (null,null,false,null,null,null);
            var gmach=await _userRepository.GetOwnedGmachAsync(userId,ct);
            return (user.Name,user.Email,gmach!=null||user.Role==UserRole.GmachAdmin,gmach?.Name,gmach?.City,gmach?.Type);
        }
        public async Task<(string? Token,string? Error)> UpdateProfileAsync(int userId,string name,string email,string currentPassword,string? newPassword,bool isOwner,string? gmachName,string? city,string? gmachType,CancellationToken ct=default)
        {
            var user=await _userRepository.GetByIdAsync(userId,ct);
            if(user==null||!_passwordService.VerifyPassword(user,currentPassword))return (null,"הסיסמה הנוכחית שגויה.");
            if(string.Equals(user.Email,_configuration["SiteAdmin:BootstrapEmail"],StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(user.Email,email.Trim(),StringComparison.OrdinalIgnoreCase))
                return (null,"שינוי כתובת המנהלת הראשית מחייב עדכון בהגדרות השרת.");
            if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(email))return (null,"יש להזין שם וכתובת מייל.");
            if(isOwner&&(string.IsNullOrWhiteSpace(gmachName)||string.IsNullOrWhiteSpace(city)))return (null,"יש להזין שם גמ״ח ועיר.");
            if(newPassword!=null&&(newPassword.Length<8||newPassword.Length>128))return (null,"הסיסמה החדשה חייבת להכיל 8 עד 128 תווים.");
            var newHash=newPassword==null?null:_passwordService.HashPassword(user,newPassword);
            var result=await _userRepository.UpdateProfileAsync(userId,name.Trim(),email.Trim().ToLowerInvariant(),newHash,isOwner,gmachName?.Trim(),city?.Trim(),gmachType?.Trim(),ct);
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

            return await IssueSessionAsync(user, cancellationToken);
        }

        public async Task<(string? Token, string? Error)> SignInWithGoogleAsync(
            string subject, string email, string name, int? signedInUserId, CancellationToken ct)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var googleUserId = await _userRepository.GetGoogleUserIdAsync(subject, ct);
            if (googleUserId > 0)
            {
                if (signedInUserId.HasValue && signedInUserId != googleUserId)
                    return (null, "חשבון Google הזה כבר מקושר לחשבון אחר באתר.");
                var linkedUser = await _userRepository.GetByIdAsync(googleUserId, ct);
                return linkedUser == null
                    ? (null, "לא ניתן להתחבר עם חשבון Google הזה.")
                    : (await IssueSessionAsync(linkedUser, ct), null);
            }

            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
            if (existingUser != null)
            {
                if (existingUser.Id != signedInUserId)
                    return (null, "כתובת המייל כבר רשומה. התחבר בסיסמה וקשר את Google דרך הפרטים שלי.");
                await _userRepository.SaveGoogleIdentityAsync(existingUser, subject, ct);
                return (await IssueSessionAsync(existingUser, ct), null);
            }

            if (signedInUserId.HasValue)
                return (null, "כדי לקשר חשבון Google יש לבחור בכתובת האימייל של החשבון באתר.");

            if (string.Equals(normalizedEmail, _configuration["SiteAdmin:BootstrapEmail"], StringComparison.OrdinalIgnoreCase) ||
                await _userRepository.RequiresReRegistrationApprovalAsync(normalizedEmail, ct))
                return (null, "החשבון הזה מחייב הרשמה מאושרת לפני התחברות דרך Google.");

            var displayName = string.IsNullOrWhiteSpace(name) ? normalizedEmail : name.Trim();
            var user = new User
            {
                Email = normalizedEmail,
                Name = displayName[..Math.Min(100, displayName.Length)],
                Role = UserRole.User
            };
            user.PasswordHash = _passwordService.HashPassword(user,
                Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            await _userRepository.SaveGoogleIdentityAsync(user, subject, ct);
            return (await IssueSessionAsync(user, ct), null);
        }

        private async Task<string> IssueSessionAsync(User user, CancellationToken cancellationToken)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is missing.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is missing.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is missing.");

            var isSiteAdmin = user.Role == UserRole.SiteAdmin ||
                string.Equals(user.Email, _configuration["SiteAdmin:BootstrapEmail"], StringComparison.OrdinalIgnoreCase);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, isSiteAdmin ? UserRole.SiteAdmin.ToString() : user.Role.ToString())
            };
            if (isSiteAdmin && (user.Role == UserRole.GmachAdmin ||
                await _userRepository.GetOwnedGmachAsync(user.Id, cancellationToken) != null))
                claims.Add(new Claim(ClaimTypes.Role, UserRole.GmachAdmin.ToString()));

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

        public Task<bool> RequiresReRegistrationApprovalAsync(string email,CancellationToken ct=default)=>_userRepository.RequiresReRegistrationApprovalAsync(email,ct);
        public Task<bool> RequestReRegistrationAsync(string email,CancellationToken ct=default)=>_userRepository.RequestReRegistrationAsync(email,ct);
        public Task<bool> RegisterAsync(RegisterDto request,CancellationToken cancellationToken=default)=>RegisterInternalAsync(request,null,cancellationToken);
        private Task<bool> RegisterInternalAsync(RegisterDto request,string? recoveryHash,CancellationToken cancellationToken)
            => RegisterImplAsync(request,recoveryHash,cancellationToken);
        private async Task<bool> RegisterImplAsync(
            RegisterDto request,
            string? recoveryHash,
            CancellationToken cancellationToken)
        {
            if(await _userRepository.RequiresReRegistrationApprovalAsync(request.Email,cancellationToken))return false;
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
                await _userRepository.AddWithGmachAsync(user, new Gmach { Name = request.GmachName!.Trim(), City = request.City!.Trim(), Type = request.GmachType?.Trim() }, cancellationToken);
            else
                await _userRepository.AddAsync(user, cancellationToken);

            return true;

        }

     

   
        
    }
}