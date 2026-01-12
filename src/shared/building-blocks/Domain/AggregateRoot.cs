namespace Platform.BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots.
/// Aggregates are the unit of consistency and transactional boundaries.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() : base() { }

    /// <summary>
    /// Version for optimistic concurrency control.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Increments the version for optimistic concurrency.
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }
}

/// <summary>
/// Aggregate root with Guid as the identifier type.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() : base() { }
}

/// <summary>
/// Marker interface for aggregate roots.
/// </summary>
public interface IAggregateRoot
{
    int Version { get; }
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
