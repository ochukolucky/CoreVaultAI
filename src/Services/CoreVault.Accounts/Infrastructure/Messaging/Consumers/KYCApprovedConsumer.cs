using System.Text;
using System.Text.Json;
using CoreVault.Accounts.Infrastructure.Persistence;
using CoreVault.Accounts.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Accounts.Infrastructure.Messaging.Consumers;

/// <summary>
/// Listens for KYCApprovedEvent from the corevault.customer exchange.
/// When a customer passes KYC verification, this consumer creates
/// a local CustomerKycSummary record in AccountsDb.
///
/// This is the Event-Carried State Transfer pattern:
/// Once this record exists, the customer is eligible to open accounts.
/// Accounts service never calls Customer service to verify KYC.
/// </summary>
public sealed class KYCApprovedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KYCApprovedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "corevault.customer";
    private const string QueueName = "accounts.kyc-approved";
    private const string RoutingKey = "kycapproved";
    private const string DeadLetterExchange = "corevault.dlx";

    public KYCApprovedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KYCApprovedConsumer> logger)
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

        _channel.ExchangeDeclare(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true);

        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

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

        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

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

        _logger.LogInformation("AccountsService listening on {Exchange} → {Queue}", ExchangeName, QueueName);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender,BasicDeliverEventArgs args)
    {
        var messageId = args.BasicProperties.MessageId;

        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());

            _logger.LogInformation("Received KYCApprovedEvent | MessageId: {MessageId}", messageId);

            // Deserialise into an anonymous type — we only need
            // the fields relevant to Accounts service
            // We do not import the full KYCApprovedEvent class
            // to avoid coupling to Customer service internals
            var payload = JsonSerializer.Deserialize<KYCApprovedPayload>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (payload is null)
            {
                _logger.LogWarning("Failed to deserialise KYCApprovedEvent | MessageId: {MessageId}", messageId);

                _channel!.BasicNack(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

            // Idempotency — if record already exists, skip
            var existing = await dbContext.CustomerKycSummaries.FirstOrDefaultAsync(
                    k => k.CustomerId == payload.CustomerId);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "KYC summary already exists for CustomerId: {CustomerId} — skipping",
                    payload.CustomerId);

                _channel!.BasicAck(
                    deliveryTag: args.DeliveryTag,
                    multiple: false);
                return;
            }

            // Create local KYC projection
            var kycSummary = CustomerKycSummary.Create(
                payload.CustomerId,
                payload.FullName,
                payload.Email,
                payload.RiskTier,
                payload.ApprovedAt);

            await dbContext.CustomerKycSummaries.AddAsync(kycSummary);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("KYC summary created for CustomerId: {CustomerId} | RiskTier: {RiskTier}",
                payload.CustomerId, payload.RiskTier);

            _channel!.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Exception processing KYCApprovedEvent | MessageId: {MessageId}", messageId);

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

/// <summary>
/// Local DTO for deserialising KYCApprovedEvent payload.
/// We use our own DTO instead of importing KYCApprovedEvent directly
/// to keep the Accounts service decoupled from Customer service internals.
/// Only the fields Accounts service actually needs are mapped here.
/// </summary>
internal sealed record KYCApprovedPayload(
    Guid CustomerId,
    string FullName,
    string Email,
    string RiskTier,
    DateTime ApprovedAt
);