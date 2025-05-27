#if MODERN
using Microsoft.EntityFrameworkCore;
using System;
using TicketSalesApp.Core.Models;
using System.Text.Json;

namespace TicketSalesApp.Core.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _provider;

        public AppDbContext(DbContextOptions<AppDbContext> options, string provider = "SQLite") : base(options)
        {
            _provider = provider;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Avtobus> Avtobusy { get; set; }
        public DbSet<Obsluzhivanie> Obsluzhivanies { get; set; }
        public DbSet<Marshut> Marshuti { get; set; }
        public DbSet<Bilet> Bilety { get; set; }
        public DbSet<Prodazha> Prodazhi { get; set; }
        public DbSet<AdminActionLog> AdminActionLogs { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RouteSchedules> RouteSchedules { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure database specific settings
            if (_provider == "SQLite")
            {
                // SQLite doesn't support decimal, so we need to convert to double
                modelBuilder.Entity<Bilet>()
                    .Property(b => b.TicketPrice)
                    .HasConversion<double>();
            }

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.GuidId)
                    .IsRequired();
                entity.HasIndex(e => e.GuidId)
                    .IsUnique();
                entity.HasIndex(e => e.Login)
                    .IsUnique();
            });

            // Configure UserRole entity
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .HasPrincipalKey(u => u.GuidId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RolePermission entity
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionId });

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Employee -> Job
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Job)
                .WithMany(j => j.Employees)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            // Marshut -> Employee (Driver)
            modelBuilder.Entity<Marshut>()
                .HasOne(m => m.Employee)
                .WithMany()
                .HasForeignKey(m => m.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Marshut -> Avtobus
            modelBuilder.Entity<Marshut>()
                .HasOne(m => m.Avtobus)
                .WithMany(b => b.Routes)
                .HasForeignKey(m => m.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bilet -> Marshut
            modelBuilder.Entity<Bilet>()
                .HasOne(b => b.Marshut)
                .WithMany(r => r.Tickets)
                .HasForeignKey(b => b.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prodazha -> Bilet
            modelBuilder.Entity<Prodazha>()
                .HasOne(p => p.Bilet)
                .WithMany(t => t.Sales)
                .HasForeignKey(p => p.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // Obsluzhivanie -> Avtobus
            modelBuilder.Entity<Obsluzhivanie>()
                .HasOne(o => o.Avtobus)
                .WithMany(b => b.Obsluzhivanies)
                .HasForeignKey(o => o.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // RouteSchedules
            modelBuilder.Entity<RouteSchedules>()
                .HasOne(rs => rs.Marshut)
                .WithMany()
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Convert string arrays to JSON for storage
            modelBuilder.Entity<RouteSchedules>()
                .Property(rs => rs.RouteStops)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<RouteSchedules>()
                .Property(rs => rs.DaysOfWeek)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<RouteSchedules>()
                .Property(rs => rs.BusTypes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<RouteSchedules>()
                .Property(rs => rs.EstimatedStopTimes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<RouteSchedules>()
                .Property(rs => rs.StopDistances)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<double[]>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<AdminActionLog>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Action)
                    .IsRequired();

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45); // для IPv6

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);
            });
        }
    }
}
#endif // MODERN
