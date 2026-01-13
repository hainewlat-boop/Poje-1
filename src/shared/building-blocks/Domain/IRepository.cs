namespace Platform.BuildingBlocks.Domain;

/// <summary>
/// Base repository interface for aggregate roots.
/// </summary>
public interface IRepository<TAggregate, TId>
    where TAggregate : IAggregateRoot
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    void Update(TAggregate aggregate);
    // NOTE: No Delete method - critical records use reversal pattern
}

/// <summary>
/// Repository interface with Guid identifier.
/// </summary>
public interface IRepository<TAggregate> : IRepository<TAggregate, Guid>
    where TAggregate : IAggregateRoot
{
}

/// <summary>
/// Unit of work pattern for transactional consistency.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
