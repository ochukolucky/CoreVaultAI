using CustomerEntity = CoreVault.Customer.Domain.Entities.Customer;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Customer.Infrastructure.Persistence;

public sealed class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options) { }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("Customers");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.UserId)
                .IsRequired();

            entity.Property(c => c.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(c => c.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(c => c.Nationality)
                .HasMaxLength(100);

            entity.Property(c => c.Address)
                .HasMaxLength(500);

            entity.Property(c => c.KYCStatus)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(c => c.RiskTier)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(c => c.KYCRejectionReason)
                .HasMaxLength(1000);

            entity.Property(c => c.RowVersion)
                .IsRowVersion();

            entity.HasIndex(c => c.UserId)
                .IsUnique();

            entity.HasIndex(c => c.Email)
                .IsUnique();
        });
    }
}