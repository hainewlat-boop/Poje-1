using MediatR;

namespace Platform.BuildingBlocks.Domain;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Name of the event type.
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base implementation for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public virtual string EventType => GetType().Name;
}
