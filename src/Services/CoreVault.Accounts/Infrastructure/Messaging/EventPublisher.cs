using System.Text;
using System.Text.Json;
using CoreVault.Contracts.Events;
using RabbitMQ.Client;

namespace CoreVault.Accounts.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : BaseEvent;
}

/// <summary>
/// Publishes domain events to RabbitMQ.
/// Same pattern as Identity EventPublisher —
/// each service has its own publisher pointing
/// to its own exchange.
/// </summary>
public sealed class EventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "corevault.accounts";

    public EventPublisher(IConfiguration configuration)
    {
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

    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent
    {
        var routingKey = @event.EventType
            .ToLower()
            .Replace("event", string.Empty);

        var payload = JsonSerializer.Serialize(
            @event,
            @event.GetType(),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var body = Encoding.UTF8.GetBytes(payload);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = @event.EventId.ToString();
        properties.Timestamp = new AmqpTimestamp(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}