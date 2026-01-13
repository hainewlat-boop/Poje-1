namespace Platform.BuildingBlocks.Outbox;

/// <summary>
/// Represents a message in the outbox for reliable event publishing.
/// The Outbox Pattern ensures domain events are published reliably
/// with transactional consistency.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string content)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            Error = null,
            RetryCount = 0
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return RetryCount < maxRetries;
    }
}

/// <summary>
/// Repository for outbox messages.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unprocessed messages for processing.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a message.
    /// </summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
