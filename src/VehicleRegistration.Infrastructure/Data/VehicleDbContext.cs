using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VehicleRegistration.Core.Entities;

namespace VehicleRegistration.Infrastructure.Data
{
    public class VehicleDbContext: DbContext
    {
        public VehicleDbContext(DbContextOptions<VehicleDbContext> options) : base(options) {}
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Owner> Owners => Set<Owner>();
        public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
        public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Owner configuration
            modelBuilder.Entity<Owner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(300);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Manufacturer configuration
            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(300);
                entity.HasIndex(e => e.Name).IsUnique();

                // Seed data
                entity.HasData(
                    new Manufacturer { Id = 1, Name = "Mazda" },
                    new Manufacturer { Id = 2, Name = "Mercedes" },
                    new Manufacturer { Id = 3, Name = "Honda" },
                    new Manufacturer { Id = 4, Name = "Ferrari" },
                    new Manufacturer { Id = 5, Name = "Toyota" }
                );
            });

            // VehicleCategory configuration
            modelBuilder.Entity<VehicleCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.MinWeight)
                    .HasPrecision(10, 2);
                entity.Property(e => e.MaxWeight)
                    .HasPrecision(10, 2);
                entity.Property(e => e.IconUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                // Category constraints: MinWeight must be non-negative, and MaxWeight must be greater than MinWeight if specified
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_VehicleCategory_MinWeight_NonNegative", "[MinWeight] >= 0");
                    t.HasCheckConstraint("CK_VehicleCategory_MaxWeight_AboveMin",
                        "[MaxWeight] IS NULL OR [MaxWeight] > [MinWeight]");
                });
                entity.HasIndex(e => e.Name).IsUnique(); //Unique constraint for category names
                entity.HasIndex(e => e.MinWeight); // lookup/order only, deliberately NOT unique

                // Seed data
                //TODO: Change the icon URLs to point to the actual icons by removing the leading slash and using relative paths, e.g., "light.svg"
                entity.HasData(
                    new VehicleCategory { Id = 1, Name = "Light", MinWeight = 0.00m, MaxWeight = 500.00m, IconUrl = "/icons/light.svg" },
                    new VehicleCategory { Id = 2, Name = "Medium", MinWeight = 500.01m, MaxWeight = 2500.00m, IconUrl = "/icons/medium.svg" },
                    new VehicleCategory { Id = 3, Name = "Heavy", MinWeight = 2500.01m, MaxWeight = null, IconUrl = "/icons/heavy.svg" }
                );
            });

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Weight)
                    .HasPrecision(10, 2);
                entity.Property(e => e.YearOfManufacture)
                    .IsRequired();

                entity.HasOne(e => e.Owner)
                    .WithMany(o => o.Vehicles)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Manufacturer)
                    .WithMany(m => m.Vehicles)
                    .HasForeignKey(e => e.ManufacturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for sorting
                entity.HasIndex(e => e.OwnerId);
                entity.HasIndex(e => e.ManufacturerId);
                entity.HasIndex(e => e.YearOfManufacture);
                entity.HasIndex(e => e.Weight);
            });
        }
    }
}
