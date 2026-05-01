namespace CoreVault.SharedKernel.ValueObjects;

/// <summary>
/// Value Objects have no identity — they are equal if their
/// properties are equal. Money(100, "MYR") == Money(100, "MYR").
/// They are always immutable. Never change a value object — 
/// create a new one.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject valueObject && Equals(valueObject);

    public override int GetHashCode() => GetEqualityComponents().Aggregate(default(int),HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is not null && left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}