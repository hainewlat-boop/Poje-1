using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Platform.BuildingBlocks.Outbox;

/// <summary>
/// Processes outbox messages and publishes them to the message bus.
/// Runs as a background job with retry logic.
/// </summary>
public interface IOutboxProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of outbox processor with RabbitMQ integration.
/// </summary>
public class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly int _batchSize;
    private readonly int _maxRetries;

    public OutboxProcessor(
        IOutboxRepository outboxRepository,
        IMessagePublisher messagePublisher,
        ILogger<OutboxProcessor> logger,
        int batchSize = 100,
        int maxRetries = 3)
    {
        _outboxRepository = outboxRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _batchSize = batchSize;
        _maxRetries = maxRetries;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.GetUnprocessedAsync(_batchSize, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _messagePublisher.PublishAsync(message.Type, message.Content, cancellationToken);
                message.MarkAsProcessed();

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId} of type {Type}",
                    message.Id,
                    message.Type);
            }
            catch (Exception ex)
            {
                message.MarkAsFailed(ex.Message);

                if (!message.CanRetry(_maxRetries))
                {
                    _logger.LogError(
                        ex,
                        "Outbox message {MessageId} exceeded max retries and will be moved to DLQ",
                        message.Id);
                    
                    // TODO: Move to dead letter queue
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to process outbox message {MessageId}, retry {RetryCount}/{MaxRetries}",
                        message.Id,
                        message.RetryCount,
                        _maxRetries);
                }
            }

            await _outboxRepository.UpdateAsync(message, cancellationToken);
        }
    }
}

/// <summary>
/// Interface for publishing messages to the message bus.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(string type, string content, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Extension methods for outbox message creation.
/// </summary>
public static class OutboxExtensions
{
    public static OutboxMessage ToOutboxMessage<T>(this T domainEvent) where T : class
    {
        var type = typeof(T).Name;
        var content = JsonSerializer.Serialize(domainEvent);
        return OutboxMessage.Create(type, content);
    }
}
