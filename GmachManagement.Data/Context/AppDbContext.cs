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
        public DbSet<DeletedAccount> DeletedAccounts { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCopy> ItemCopies { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<EmailReminderDelivery> EmailReminderDeliveries { get; set; }
        public DbSet<ManagedLoan> ManagedLoans { get; set; }
        public DbSet<BorrowRequest> BorrowRequests { get; set; }
        public DbSet<PersonalTransaction> PersonalTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DeletedAccount>(e=>{e.Property(x=>x.Email).HasMaxLength(254).IsRequired();e.HasIndex(x=>x.Email).IsUnique();});

            modelBuilder.Entity<Gmach>(entity => {
                entity.Property(g => g.Name).HasMaxLength(150).IsRequired();
                entity.Property(g => g.City).HasMaxLength(100).IsRequired();
                entity.Property(g => g.Type).HasMaxLength(100).IsRequired().HasDefaultValue("");
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
            modelBuilder.Entity<EmailReminderDelivery>(e => { e.Property(x=>x.Kind).HasMaxLength(20).IsRequired(); e.HasIndex(x=>new {x.Kind,x.ReferenceId,x.ReminderDay}).IsUnique(); });
            modelBuilder.Entity<ManagedLoan>(entity => { entity.HasKey(x=>x.Id); entity.Property(x=>x.BorrowerName).HasMaxLength(150).IsRequired(); entity.Property(x=>x.BorrowerPhone).HasMaxLength(30); entity.Property(x=>x.BorrowerEmail).HasMaxLength(254); entity.HasIndex(x=>x.GmachId); entity.HasIndex(x=>x.ItemCopyId); });
            modelBuilder.Entity<BorrowRequest>(entity =>
            {
                entity.Property(request => request.ApplicantName).HasMaxLength(150).IsRequired();
                entity.Property(request => request.Phone).HasMaxLength(30);
                entity.Property(request => request.Email).HasMaxLength(254);
                // Concurrent approvals of the same request must only create one loan.
                entity.Property(request => request.FulfilledAt).IsConcurrencyToken();
                entity.HasOne(request => request.Item)
                    .WithMany()
                    .HasForeignKey(request => request.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(request => request.ItemId);
            });
            SeedData.Seed(modelBuilder);
        }
    }
}
