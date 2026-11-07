
using GmachManagement.Service.Mappings;
using Microsoft.Extensions.DependencyInjection;
using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Services;

using GmachManagement.Service.Security;

namespace GmachManagement.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServiceServices(
            this IServiceCollection services)
        {
            services.AddAutoMapper(

                cfg => cfg.AddProfile<MappingProfile>());

            services.AddScoped<GmachService>();
            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<PasswordService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPersonalTransactionService, PersonalTransactionService>();

            services.AddScoped<ICategoryService, CategoryService>();

            return services;
        }
    }
}
