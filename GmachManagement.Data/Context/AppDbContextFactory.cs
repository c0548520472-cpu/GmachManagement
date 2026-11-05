using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GmachManagement.Data.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // The API owns the development secrets. The Data project must not
            // reference the API project to read them.
            const string apiUserSecretsId = "756bb929-c18a-4270-bac8-770f68b1d44c";

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(apiUserSecretsId, optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = configuration["DatabaseProvider"] ?? "SqlServer";
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("PostgreSQL")
                    ?? throw new InvalidOperationException("PostgreSQL connection string is missing from API user secrets or environment.");
                optionsBuilder.UseNpgsql(connectionString);
            }
            else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("DefaultConnection is missing.");
                optionsBuilder.UseSqlServer(connectionString);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported database provider: {provider}");
            }

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
