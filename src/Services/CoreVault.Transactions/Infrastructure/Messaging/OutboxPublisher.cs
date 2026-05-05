using System.Text;
using CoreVault.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace CoreVault.Transactions.Infrastructure.Messaging;

/// <summary>
/// Background service that reads unpublished OutboxMessages
/// and publishes them to RabbitMQ.
///
/// Runs every second — picks up to 20 messages per cycle.
/// On success → marks message as ProcessedAt = now
/// On failure → increments RetryCount
/// After 3 retries → message stays unprocessed
///                   monitoring system alerts ops team
///
/// This is the second half of the Outbox Pattern:
/// Handler writes to OutboxMessages table (atomic with business data)
/// This publisher reads and delivers to RabbitMQ (reliable delivery)
/// </summary>
public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly RabbitMQ.Client.IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;
    private const string ExchangeName = "corevault.transactions";

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisher> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started — polling every 1 second");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"OutboxPublisher encountered an error");
            }

            // Wait 1 second before next poll
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        // Fetch unprocessed messages that have not exceeded retry limit
        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!pending.Any()) return;

        _logger.LogInformation("OutboxPublisher processing {Count} pending messages",pending.Count);

        foreach (var message in pending)
        {
            try
            {
                // Derive routing key from event type
                // TransactionInitiatedEvent → transaction.initiated
                var routingKey = message.EventType
                    .ToLower()
                    .Replace("event", string.Empty)
                    .Insert(message.EventType
                        .ToLower()
                        .Replace("event", string.Empty)
                        .LastIndexOf(char.IsUpper(
                            message.EventType
                                .Replace("Event", "")[^1])
                            ? 'a' : 'a'), ".")
                    .Trim('.');

                // Simpler routing key derivation
                routingKey = DeriveRoutingKey(message.EventType);

                var body = Encoding.UTF8.GetBytes(message.Payload);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = message.Id.ToString();
                properties.Timestamp = new AmqpTimestamp(
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Headers = new Dictionary<string, object>
                {
                    { "eventType", message.EventType }
                };

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                message.MarkAsProcessed();

                _logger.LogInformation(
                    "Published {EventType} | MessageId: {MessageId} | RoutingKey: {RoutingKey}",
                    message.EventType, message.Id, routingKey);
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message);

                _logger.LogError(ex,
                    "Failed to publish {EventType} | MessageId: {MessageId} | RetryCount: {RetryCount}",
                    message.EventType, message.Id, message.RetryCount);
            }
        }

        // Save all ProcessedAt and RetryCount updates
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Derives RabbitMQ routing key from event type name.
    /// TransactionInitiatedEvent → transaction.initiated
    /// TransactionCompletedEvent → transaction.completed
    /// FraudDetectedEvent        → fraud.detected
    /// </summary>
    private static string DeriveRoutingKey(string eventType)
    {
        // Remove "Event" suffix
        var name = eventType.Replace("Event", string.Empty);

        // Insert dot before each uppercase letter (except first)
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result.Append('.');
            result.Append(char.ToLower(name[i]));
        }

        return result.ToString();
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}