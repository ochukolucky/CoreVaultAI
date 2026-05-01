using CoreVault.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CoreVault.Identity.Infrastructure.Persistence;

/// <summary>
/// Extends IdentityDbContext which gives us all Identity tables:
/// AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims etc.
/// We configure our custom ApplicationUser mappings on top.
/// </summary>
public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Must call base — sets up Identity tables

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>(); // Store as "Customer" not "1"

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.RefreshToken)
                .HasMaxLength(500);

            // Optimistic concurrency — prevents race conditions
            entity.Property(u => u.ConcurrencyStamp)
                .IsRowVersion();

            // Index on email for fast login lookups
            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        // Rename Identity tables to cleaner names
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}