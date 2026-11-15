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

        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCopy> ItemCopies { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<BorrowRequest> BorrowRequests { get; set; }
        public DbSet<PersonalTransaction> PersonalTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });



            modelBuilder.Entity<Item>()
                .HasOne(i => i.Owner).WithMany().HasForeignKey(i => i.OwnerId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BorrowRequest>(entity =>
            {
                entity.Property(r => r.ApplicantName).HasMaxLength(100).IsRequired();
                entity.Property(r => r.Phone).HasMaxLength(30);
                entity.Property(r => r.Email).HasMaxLength(254);
                entity.HasOne(r => r.ItemCopy).WithMany().HasForeignKey(r => r.ItemCopyId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Item>()
                .HasMany(i => i.Copies)
                .WithOne(c => c.Item)
                .HasForeignKey(c => c.ItemId);

            modelBuilder.Entity<Item>()
                .HasMany(i => i.Categories)
                .WithMany(c => c.Items);

            modelBuilder.Entity<ItemCopy>()
                .Property(c => c.RowVersion)
                .IsRowVersion();

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
