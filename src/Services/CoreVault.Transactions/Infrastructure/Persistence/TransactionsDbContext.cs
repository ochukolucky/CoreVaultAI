using CoreVault.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Transactions.Infrastructure.Persistence;

public sealed class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options)
        : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AccountSummary> AccountSummaries => Set<AccountSummary>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Transaction ──────────────────────────────────────────
        builder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.AccountId)
                .IsRequired();

            entity.Property(t => t.CustomerId)
                .IsRequired();

            entity.Property(t => t.Type)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(t => t.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(t => t.Reference)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.DeviceFingerprint)
                .HasMaxLength(500);

            entity.Property(t => t.IpAddress)
                .HasMaxLength(50);

            entity.Property(t => t.Location)
                .HasMaxLength(200);

            entity.Property(t => t.FailureReason)
                .HasMaxLength(500);

            entity.Property(t => t.FraudReason)
                .HasMaxLength(1000);

            // Optimistic concurrency
            entity.Property(t => t.RowVersion)
                .IsRowVersion();

            // Unique — no duplicate transactions
            entity.HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            // Fast lookup by AccountId
            entity.HasIndex(t => t.AccountId);

            // Fast lookup by CustomerId
            entity.HasIndex(t => t.CustomerId);

            // Background job queries Pending transactions
            entity.HasIndex(t => t.Status);
        });

        // ── OutboxMessage ────────────────────────────────────────
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(o => o.Payload)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.Property(o => o.ErrorMessage)
                .HasMaxLength(1000);

            // Background publisher queries unprocessed messages
            entity.HasIndex(o => o.ProcessedAt);
            entity.HasIndex(o => o.RetryCount);
        });

        // ── AccountSummary ───────────────────────────────────────
        builder.Entity<AccountSummary>(entity =>
        {
            entity.ToTable("AccountSummaries");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.AccountId)
                .IsRequired();

            entity.Property(a => a.CustomerId)
                .IsRequired();

            entity.Property(a => a.AccountNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(a => a.AccountType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(a => a.DailyTransactionLimit)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // One summary per account
            entity.HasIndex(a => a.AccountId)
                .IsUnique();

            // Fast lookup by CustomerId —
            // get all accounts for a customer
            entity.HasIndex(a => a.CustomerId);

            // Fast lookup by AccountNumber —
            // resolve AccountId from account number
            entity.HasIndex(a => a.AccountNumber)
                .IsUnique();
        });
    }
}