
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using GmachManagement.Data.Repositories;

namespace GmachManagement.Data
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString(
                        "DefaultConnection")));

            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            return services;
        }


        public static async Task CreateInitialAdminAsync(
            IServiceProvider serviceProvider,
            string email,
            string password)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            await Configurations.AdminSeeder.SeedAdminAsync(
                context,
                email,
                password);
        }

    }
}
