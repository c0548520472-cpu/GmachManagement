
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using GmachManagement.Data.Repositories;
using GmachManagement.Data.Services;

namespace GmachManagement.Data
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                var provider = configuration["DatabaseProvider"] ?? "SqlServer";
                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")
                        ?? throw new InvalidOperationException("PostgreSQL connection string is missing."));
                }
                else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported database provider: {provider}");
                }
            });

            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalTransactionRepository, PersonalTransactionRepository>();
            services.AddHostedService<PersonalReminderWorker>();
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
