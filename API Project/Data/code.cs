using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Project.Data
{
    public class EquipmentManagementContext : DbContext
    {
        public EquipmentManagementContext(DbContextOptions<EquipmentManagementContext> options)
            : base(options)
        {
        }

        // DbSet for each entity
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentCheckout> EquipmentCheckouts { get; set; }
        public DbSet<Blacklist> Blacklists { get; set; }
        public DbSet<EquipmentRequest> EquipmentRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Team configurations
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasIndex(t => t.TeamName).IsUnique();
            });

            // Equipment configurations
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasDefaultValue("Available");
            });

            // Equipment Checkout configurations
            modelBuilder.Entity<EquipmentCheckout>(entity =>
            {
                entity.Property(ec => ec.Status)
                    .HasDefaultValue("CheckedOut");

                entity.HasOne<Equipment>()
                    .WithMany()
                    .HasForeignKey(ec => ec.EquipmentID);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(ec => ec.TeamID);
            });

            // Equipment Request configurations
            modelBuilder.Entity<EquipmentRequest>(entity =>
            {
                entity.Property(er => er.Status)
                    .HasDefaultValue("Pending");

                entity.Property(er => er.Urgency)
                    .HasDefaultValue("Normal");
            });

            modelBuilder.Entity<UserRoleAssignment>(entity =>
            {
                // Composite primary key
                entity.HasKey(ura => new { ura.UserID, ura.RoleID });

                // Relationships
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(ura => ura.UserID);

                entity.HasOne<UserRole>()
                    .WithMany()
                    .HasForeignKey(ura => ura.RoleID);

                // Default assigned date
                entity.Property(ura => ura.AssignedDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed initial user roles
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { RoleID = 1, RoleName = "TeamMember", Description = "Regular team member" },
                new UserRole { RoleID = 2, RoleName = "WarehouseOperative", Description = "Warehouse staff who can issue equipment" },
                new UserRole { RoleID = 3, RoleName = "WarehouseManager", Description = "Warehouse manager who can manage equipment inventory" },
                new UserRole { RoleID = 4, RoleName = "CentralManager", Description = "Central manager of the nest" }
            );
        }
    }
}

