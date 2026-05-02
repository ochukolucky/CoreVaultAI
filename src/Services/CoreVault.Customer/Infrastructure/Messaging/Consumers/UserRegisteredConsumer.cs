using System.Text;
using System.Text.Json;
using CoreVault.Contracts.Events.Identity;
using CoreVault.Customer.Application.Commands.CreateCustomer;
using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CoreVault.Customer.Infrastructure.Messaging.Consumers;

/// <summary>
/// Listens to the corevault.identity exchange for UserRegisteredEvent.
/// When Identity publishes a new user registration, this consumer
/// automatically creates the matching Customer profile.
///
/// This is the event-driven architecture in action:
///   Identity service has NO knowledge this consumer exists.
///   Customer service has NO reference to Identity service code.
///   They are connected only through the event contract.
/// </summary>
public sealed class UserRegisteredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "corevault.identity";
    private const string QueueName = "customer.user-registered";
    private const string RoutingKey = "userregistered";
    private const string DeadLetterExchange = "corevault.dlx";

    public UserRegisteredConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<UserRegisteredConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare Dead Letter Exchange
        _channel.ExchangeDeclare(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true);

        // Declare Identity exchange — idempotent, safe to call again
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

        // Declare queue with DLX configured
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", $"dead.{QueueName}" },
                { "x-message-ttl", 86400000 }
            });

        // Bind queue to exchange
        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

        // Process one message at a time
        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "CustomerService listening on {Exchange} → {Queue}",
            ExchangeName, QueueName);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(
        object sender,
        BasicDeliverEventArgs args)
    {
        var messageId = args.BasicProperties.MessageId;

        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());

            _logger.LogInformation(
                "Received UserRegisteredEvent | MessageId: {MessageId}",
                messageId);

            var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (@event is null)
            {
                _logger.LogWarning(
                    "Failed to deserialise UserRegisteredEvent | MessageId: {MessageId}",
                    messageId);

                _channel!.BasicNack(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var command = new CreateCustomerCommand(
                @event.UserId,
                @event.FirstName,
                @event.LastName,
                @event.Email);

            var result = await mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Customer profile created | CustomerId: {CustomerId} | UserId: {UserId}",
                    result.Value.Id, @event.UserId);

                _channel!.BasicAck(
                    deliveryTag: args.DeliveryTag,
                    multiple: false);
            }
            else
            {
                _logger.LogError(
                    "Failed to create customer | Error: {Error}",
                    result.Error);

                _channel!.BasicNack(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception processing UserRegisteredEvent | MessageId: {MessageId}",
                messageId);

            _channel!.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}