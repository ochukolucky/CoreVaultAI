namespace CoreVault.SharedKernel.Entities;

/// <summary>
/// Every entity in every service inherits from this.
/// Provides identity, audit timestamps, and soft delete.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Entity Id cannot be empty.", nameof(id));

        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    // Required by EF Core — never call directly
    protected BaseEntity() { }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Concurrency token — prevents race conditions on balance updates
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;

    internal void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}