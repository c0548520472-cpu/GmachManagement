using System;
using System.Collections.Generic;
using System.Text;

using GmachManagement.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using GmachManagement.Core.Entities;


namespace GmachManagement.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options) : base(options)
           
        {
        }

        // Allows a provider-specific design-time context to reuse this model.
        protected AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Gmach> Gmachs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCopy> ItemCopies { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<PersonalTransaction> PersonalTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Gmach>(entity => {
                entity.Property(g => g.Name).HasMaxLength(150).IsRequired();
                entity.Property(g => g.City).HasMaxLength(100).IsRequired();
                entity.HasIndex(g => g.OwnerId).IsUnique();
                entity.HasOne(g => g.Owner).WithOne(u => u.OwnedGmach).HasForeignKey<Gmach>(g => g.OwnerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(g => g.Items).WithOne(i => i.Gmach).HasForeignKey(i => i.GmachId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });



            modelBuilder.Entity<Item>()
                .HasMany(i => i.Copies)
                .WithOne(c => c.Item)
                .HasForeignKey(c => c.ItemId);

            modelBuilder.Entity<Item>()
                .HasMany(i => i.Categories)
                .WithMany(c => c.Items);

            // SQL Server uses its native rowversion; PostgreSQL uses the xmin system column.
            // Ignore the SQL Server-only byte[] property for PostgreSQL so its
            // [Timestamp] annotation does not create an incompatible bytea column.
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<ItemCopy>().Ignore(c => c.RowVersion);
                modelBuilder.Entity<ItemCopy>()
                    .Property<uint>("xmin")
                    .IsRowVersion();
            }
            else
            {
                modelBuilder.Entity<ItemCopy>()
                    .Property(c => c.RowVersion)
                    .IsRowVersion();
            }

            modelBuilder.Entity<User>()
                .HasMany(u => u.Loans)
                .WithOne(l => l.User)
                .HasForeignKey(l => l.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.PersonalTransactions)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.ItemCopy)
                .WithMany(c => c.Loans)
                .HasForeignKey(l => l.ItemCopyId);
            SeedData.Seed(modelBuilder);
        }
    }
}
