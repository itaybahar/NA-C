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

        public DbSet<CheckoutRecord> CheckoutRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Team configurations
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasIndex(t => t.TeamName).IsUnique();
                entity.Property(t => t.IsActive).HasDefaultValue(true);
                entity.Property(t => t.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(t => t.IsBlacklisted).HasDefaultValue(false);
            });

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Equipment configurations
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasDefaultValue(EquipmentStatus.Available);

                // Explicitly map Id as primary key
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<EquipmentCheckout>(entity =>
            {
                // Define primary key
                entity.HasKey(ec => ec.CheckoutID);

                // Map database column explicitly
                entity.Property(ec => ec.EquipmentId)
                    .HasColumnName("EquipmentID");

                entity.Property(ec => ec.Status)
                    .HasDefaultValue("CheckedOut");

                // Fix Equipment relationship - explicitly define the foreign key
                entity.HasOne(ec => ec.Equipment) // Navigation property
                    .WithMany() // Assuming Equipment has no collection of EquipmentCheckouts
                    .HasForeignKey(ec => ec.EquipmentId) // Explicitly map the foreign key
                    .HasPrincipalKey(e => e.Id) // Map to the primary key of Equipment
                    .OnDelete(DeleteBehavior.Cascade) // Define delete behavior
                    .IsRequired();

                // Configure Team relationship
                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(ec => ec.TeamID);

                // Make nullable fields explicit
                entity.Property(ec => ec.CheckoutDate).IsRequired();
                entity.Property(ec => ec.ExpectedReturnDate).IsRequired();
                entity.Property(ec => ec.Status).HasMaxLength(20).IsRequired();
                entity.Property(ec => ec.Quantity).IsRequired().HasDefaultValue(1);
            });

            // Equipment Request configurations
            modelBuilder.Entity<EquipmentRequest>(entity =>
            {
                entity.Property(er => er.Status)
                    .HasDefaultValue("Pending");

                entity.Property(er => er.Urgency)
                    .HasDefaultValue("Normal");
            });

            // UserRoleAssignment configuration
            modelBuilder.Entity<UserRoleAssignment>(entity =>
            {
                // Define composite primary key
                entity.HasKey(ura => new { ura.UserID, ura.RoleID });

                // Configure User relationship with explicit foreign key name
                entity.HasOne(ura => ura.User)
                    .WithMany()
                    .HasForeignKey(ura => ura.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure UserRole relationship with explicit foreign key name
                entity.HasOne(ura => ura.UserRole)
                    .WithMany()
                    .HasForeignKey(ura => ura.RoleID)
                    .OnDelete(DeleteBehavior.Cascade);

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
