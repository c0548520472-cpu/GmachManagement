using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GmachManagement.Data.Context
{
    public sealed class PostgreSqlMigrationsDbContextFactory
        : IDesignTimeDbContextFactory<PostgreSqlMigrationsDbContext>
    {
        public PostgreSqlMigrationsDbContext CreateDbContext(string[] args)
        {
            // Same API development secrets used by the existing AppDbContextFactory.
            const string apiUserSecretsId = "756bb929-c18a-4270-bac8-770f68b1d44c";

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(apiUserSecretsId)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "PostgreSQL connection string is missing from API user secrets or environment.");

            var options = new DbContextOptionsBuilder<PostgreSqlMigrationsDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new PostgreSqlMigrationsDbContext(options);
        }
    }
}
