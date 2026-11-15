using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Context
{
    // Dedicated EF context type so PostgreSQL migrations have their own snapshot.
    // Inherits the same entities and model configuration as AppDbContext.
    public sealed class PostgreSqlMigrationsDbContext : AppDbContext
    {
        public PostgreSqlMigrationsDbContext(DbContextOptions<PostgreSqlMigrationsDbContext> options)
            : base(options)
        {
        }
    }
}
