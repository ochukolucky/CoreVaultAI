using AccountEntity = CoreVault.Accounts.Domain.Entities.Account;
using CoreVault.Accounts.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace CoreVault.Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
        : base(options) { }

    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<CustomerKycSummary> CustomerKycSummaries => Set<CustomerKycSummary>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Account ──────────────────────────────────────────────
        builder.Entity<AccountEntity>(entity =>
        {
            entity.ToTable("Accounts");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.CustomerId)
                .IsRequired();

            entity.Property(a => a.AccountNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(a => a.AccountType)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(a => a.Balance)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(a => a.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(a => a.InterestRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(a => a.DailyTransactionLimit)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(a => a.CloseReason)
                .HasMaxLength(500);

            // Optimistic concurrency — critical for balance updates
            entity.Property(a => a.RowVersion)
                .IsRowVersion();

            // Fast lookup by CustomerId — get all accounts for a customer
            entity.HasIndex(a => a.CustomerId);

            // Account number must be unique across all accounts
            entity.HasIndex(a => a.AccountNumber)
                .IsUnique();
        });

        // ── CustomerKycSummary ───────────────────────────────────
        builder.Entity<CustomerKycSummary>(entity =>
        {
            entity.ToTable("CustomerKycSummaries");

            entity.HasKey(k => k.Id);

            entity.Property(k => k.CustomerId)
                .IsRequired();

            entity.Property(k => k.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(k => k.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(k => k.RiskTier)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(k => k.VerifiedAt)
                .IsRequired();

            // One KYC summary per customer
            entity.HasIndex(k => k.CustomerId)
                .IsUnique();
        });
    }
}