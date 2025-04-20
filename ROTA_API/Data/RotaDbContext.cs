using Microsoft.EntityFrameworkCore;
using ROTA_API.Models;

namespace ROTA_API.Data
{
        public class RotaDbContext : DbContext
        {
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ShiftType> ShiftTypes { get; set; } = null!;
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<LeaveType> LeaveTypes { get; set; } = null!;
        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;

        public RotaDbContext(DbContextOptions<RotaDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(t => t.TeamId);
                entity.HasIndex(t => t.TeamName).IsUnique(); // Unique constraint on name
                entity.Property(t => t.TeamName).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Team) // Employee has one Team (or null)
                .WithMany(t => t.Employees) // Team has many Employees
                .HasForeignKey(e => e.TeamId) // The FK property in Employee
                .OnDelete(DeleteBehavior.SetNull); // If Team deleted, set Employee.TeamId to null
                                                   // --- End Configure Employee-Team ---

            modelBuilder.Entity<ShiftType>()
               .Property(st => st.IsOnCall)
               .IsRequired() // Make sure it's not nullable in DB
               .HasDefaultValue(false); // Set DB default if desired (EF Core handles default in code anyway)


            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<ShiftType>()
               .HasIndex(st => st.TypeName)
               .IsUnique();

            modelBuilder.Entity<LeaveType>()
                .HasIndex(lt => lt.LeaveTypeName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Shifts)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Shift>()
               .HasOne(s => s.ShiftType)
               .WithMany(st => st.Shifts)
               .HasForeignKey(s => s.ShiftTypeId)
               .OnDelete(DeleteBehavior.SetNull); // Or Restrict if you don't want shifts without types

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.LeaveType)
                .WithMany(lt => lt.LeaveRequests)
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.CreatedByUser)
                .WithMany(u => u.CreatedShifts)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Shift>()
                .HasOne(s => s.UpdatedByUser)
                .WithMany(u => u.UpdatedShifts)
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LeaveRequest>()
               .HasOne(lr => lr.ApproverUser)
               .WithMany(u => u.ApprovedLeaveRequests)
               .HasForeignKey(lr => lr.ApproverUserId)
               .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LeaveRequest>()
               .HasOne(lr => lr.CreatedByUser)
               .WithMany(u => u.CreatedLeaveRequests)
               .HasForeignKey(lr => lr.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LeaveRequest>()
               .HasOne(lr => lr.UpdatedByUser)
               .WithMany(u => u.UpdatedLeaveRequests)
               .HasForeignKey(lr => lr.UpdatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

           

            modelBuilder.Entity<Role>().HasData(
                    new Role
                    {
                        RoleId = 1,
                        RoleName = "Admin",
                        CanEditRota = true,
                        CanEditLeave = true,
                        CanApproveLeave = true,
                        CanViewRota = true,
                        CanViewLeave = true,
                        Description = "Full access administrator",
                        
                    },
                    new Role
                    {
                        RoleId = 2,
                        RoleName = "Viewer",
                        CanEditRota = false,
                        CanEditLeave = false,
                        CanApproveLeave = false,
                        CanViewRota = true,
                        CanViewLeave = true,
                        Description = "Standard employee view-only access"
                    }
                );
            }
        }
    }

