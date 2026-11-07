
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

            if (string.Equals(configuration["DatabaseProvider"], "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<PostgreSqlMigrationsDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));
            }

            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalTransactionRepository, PersonalTransactionRepository>();
            services.AddHostedService<PersonalReminderWorker>();
            services.AddScoped<IGmachRepository, GmachRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            return services;
        }


        public static async Task ApplyPostgreSqlMigrationsAsync(IServiceProvider provider, IConfiguration configuration)
        {
            if (!string.Equals(configuration["DatabaseProvider"], "PostgreSQL", StringComparison.OrdinalIgnoreCase)) return;
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PostgreSqlMigrationsDbContext>();
            await context.Database.MigrateAsync();
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
