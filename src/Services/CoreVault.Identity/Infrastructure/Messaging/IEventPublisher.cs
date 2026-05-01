using System.Text;
using System.Text.Json;
using CoreVault.Contracts.Events;
using Microsoft.Extensions.Configuration; 
using RabbitMQ.Client;

namespace CoreVault.Identity.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent;
}

public sealed class EventPublisher : IEventPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _factory;
    private const string ExchangeName = "corevault.identity";

    public EventPublisher(IConfiguration configuration)
    {
        // Setup factory - DispatchConsumersAsync is no longer needed
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
    }

    private async Task InitializeAsync()
    {
        if (_channel is not null) return;

        // Connections and Channels are now created asynchronously
        _connection = await _factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent
    {
        await InitializeAsync();

        var routingKey = @event.EventType.ToLower().Replace("event", string.Empty);
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var body = Encoding.UTF8.GetBytes(payload);

        // BasicProperties are handled differently in v7
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        // BasicPublish is now BasicPublishAsync
        await _channel!.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}