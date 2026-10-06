using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TemsGroupProject.Models;

namespace TemsGroupProject.Data
{
    // IdentityDbContext<TUser, TRole, TKey> with TKey = int gives every Identity table
    // (Users, Roles, UserRoles, etc.) an int primary key instead of the default GUID.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public DbSet<FeeRequest> FeeRequests { get; set; }

        // <-- ADDED: Phase Two auditing tables
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
                b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
                b.Property(u => u.StaffId).HasMaxLength(50).IsRequired();
                b.HasIndex(u => u.StaffId).IsUnique();
            });

            builder.Entity<FeeRequest>().ToTable("Requests");

            // <-- ADDED: map the two new auditing entities to explicit table names
            builder.Entity<AuditLog>().ToTable("AuditLogs");
            builder.Entity<UserSession>().ToTable("UserSessions");

            // Rename tables to match the plain "Users" style table your doc describes,
            // instead of the default AspNetUsers / AspNetRoles names. Optional — remove
            // this block if you're fine with the default Identity table names.
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole<int>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        }
    }
}