
using GmachManagement.API.Controllers;
using GmachManagement.API.Middlewares;
using GmachManagement.API.Services;
using GmachManagement.Core.Interfaces;
using GmachManagement.Service;
using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using System.Reflection;
using System.Text;

namespace GmachManagement.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRateLimiter(options => {
    options.AddPolicy("password-recovery", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("gmach-chat-send", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions {
                PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
            }));
    options.AddPolicy("site-activity", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions {
                PermitLimit = 90, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
            }));
    options.RejectionStatusCode = 429;
});
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
            builder.Services.AddServiceServices();

            var dataAssemblyPath = Path.Combine(
                AppContext.BaseDirectory,
                "GmachManagement.Data.dll");

            var dataAssembly = Assembly.LoadFrom(dataAssemblyPath);


            var dependencyInjectionType = dataAssembly.GetType(
                "GmachManagement.Data.DependencyInjection")
                ?? throw new InvalidOperationException(
                    "Data DependencyInjection class not found.");

            var addDataServicesMethod = dependencyInjectionType.GetMethod(
                "AddDataServices",
                BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException(
                    "AddDataServices method not found.");

            addDataServicesMethod.Invoke(
                null,
                new object[] { builder.Services, builder.Configuration });

            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Add services to the container.

            
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is missing.");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is missing.");

            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing.");

            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT Key must contain at least 32 bytes.");
            }

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents {
                        OnTokenValidated = async context => {
                            var idText=context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            if(!int.TryParse(idText,out var id)) { context.Fail("Invalid user."); return; }
                            var repository=context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                            var user=await repository.GetByIdAsync(id,context.HttpContext.RequestAborted);
                            if(user==null){ context.Fail("Account deleted."); return; }
                            var adminClaim=context.Principal?.IsInRole("SiteAdmin")==true;
                            var currentAdmin=user.Role==GmachManagement.Core.Enums.UserRole.SiteAdmin ||
                                string.Equals(user.Email,builder.Configuration["SiteAdmin:BootstrapEmail"],StringComparison.OrdinalIgnoreCase);
                            if(adminClaim!=currentAdmin){context.Fail("Permissions changed; sign in again.");return;}
                            var ownerClaim=context.Principal?.IsInRole("GmachAdmin")==true;
                            var currentOwner=user.Role==GmachManagement.Core.Enums.UserRole.GmachAdmin ||
                                (currentAdmin && await repository.GetOwnedGmachAsync(id,context.HttpContext.RequestAborted)!=null);
                            if(ownerClaim!=currentOwner){context.Fail("Permissions changed; sign in again.");return;}
                        }
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddDataProtection().SetApplicationName("GmachManagement");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ClientPolicy", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5500",
                                     "http://127.0.0.1:5500",
                                     "https://gmachmanagement-client.pages.dev")
                        // Cloudflare Pages preview deployments use a unique subdomain.
                        // Accept only our Pages project and its previews, not arbitrary pages.dev sites.
                        .SetIsOriginAllowed(origin =>
                        {
                            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                                return false;
                            if (uri.Scheme == "http" &&
                                (uri.Host == "localhost" || uri.Host == "127.0.0.1") &&
                                uri.Port == 5500)
                                return true;
                            return uri.Scheme == "https" &&
                                (uri.Host.Equals("gmachmanagement-client.pages.dev", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.EndsWith(".gmachmanagement-client.pages.dev", StringComparison.OrdinalIgnoreCase));
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            builder.Services.AddScoped<IItemService, ItemService>();
            builder.Services.AddScoped<GmachCommunicationService>();
            builder.Services.AddHostedService<UnreadChatReminderWorker>();
            builder.Services.AddHttpClient("GmachChatEmail", client => client.Timeout = TimeSpan.FromSeconds(8));
            builder.Services.AddHttpClient<ResendEmailService>(client => client.Timeout = TimeSpan.FromSeconds(12));


            var app = builder.Build();

            // Apply only pending PostgreSQL migrations before serving requests.
            // Data stays loaded through reflection; API does not reference Data.
            var migrationMethod = dependencyInjectionType.GetMethod(
                "ApplyPostgreSqlMigrationsAsync", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("Data migration runner not found.");
            await (Task)(migrationMethod.Invoke(null, new object[] { app.Services, app.Configuration })
                ?? throw new InvalidOperationException("Migration runner returned no task."));

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            app.UseMiddleware<CorrelationIdMiddleware>();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }


            app.UseHttpsRedirection();

            app.UseCors("ClientPolicy");

            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();



            app.MapControllers();

           
            app.Run();
        }
    }
}
