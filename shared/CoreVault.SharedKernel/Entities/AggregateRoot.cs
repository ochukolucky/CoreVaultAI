namespace CoreVault.SharedKernel.Entities;

/// <summary>
/// An Aggregate Root is the top-level entity in a DDD aggregate.
/// All access to entities within the aggregate goes through the root.
/// Example: Account is the root. Transactions belong to Account.
/// Nobody touches a Transaction directly — they go through Account.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    protected AggregateRoot(Guid id) : base(id) { }

    // Required by EF Core
    protected AggregateRoot() { }
}