
using GmachManagement.API.Controllers;
using GmachManagement.API.Middlewares;
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
                });

            builder.Services.AddAuthorization();
            builder.Services.AddDataProtection().SetApplicationName("GmachManagement");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ClientPolicy", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5500",
                                     "http://127.0.0.1:5500")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            builder.Services.AddScoped<IItemService, ItemService>();


            var app = builder.Build();

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
            app.UseAuthorization();



            app.MapControllers();

           
            app.Run();
        }
    }
}
