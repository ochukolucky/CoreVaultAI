using System.Text;
using System.Text.Json;
using CoreVault.Transactions.Domain.Entities;
using CoreVault.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CoreVault.Transactions.Infrastructure.Messaging.Consumers;

/// <summary>
/// Listens for AccountOpenedEvent from corevault.accounts exchange.
/// Builds the local AccountSummary projection used by the
/// Transaction Service for ownership validation and account
/// number resolution.
///
/// This is Event-Carried State Transfer — same pattern as
/// KYCApprovedConsumer in Accounts Service.
/// Transaction Service never calls Accounts Service directly.
/// </summary>
public sealed class AccountOpenedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountOpenedConsumer> _logger;
    private RabbitMQ.Client.IConnection? _connection;
    private RabbitMQ.Client.IModel? _channel;

    private const string ExchangeName = "corevault.accounts";
    private const string QueueName = "transactions.account-opened";
    private const string RoutingKey = "accountopened";
    private const string DeadLetterExchange = "corevault.dlx";

    public AccountOpenedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AccountOpenedConsumer> logger)
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

        _logger.LogInformation(
            "TransactionService listening on {Exchange} → {Queue}",
            ExchangeName, QueueName);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var messageId = args.BasicProperties.MessageId;

        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());

            _logger.LogInformation("Received AccountOpenedEvent | MessageId: {MessageId}",messageId);

            // Tolerant Reader Pattern — only map fields we need
            var payload = JsonSerializer.Deserialize<AccountOpenedPayload>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (payload is null)
            {
                _logger.LogWarning("Failed to deserialise AccountOpenedEvent | MessageId: {MessageId}",
                    messageId);

                _channel!.BasicNack(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

            // Idempotency check
            var existing = await dbContext.AccountSummaries
                .FirstOrDefaultAsync(
                    a => a.AccountId == payload.AccountId);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "AccountSummary already exists for AccountId: {AccountId} — skipping",
                    payload.AccountId);

                _channel!.BasicAck(
                    deliveryTag: args.DeliveryTag,
                    multiple: false);
                return;
            }

            var summary = AccountSummary.Create(
                payload.AccountId,
                payload.CustomerId,
                payload.AccountNumber,
                payload.AccountType,
                "Active",
                payload.Currency,
                50000m); // Default daily limit
                         // In production this comes from
                         // the AccountOpenedEvent payload

            await dbContext.AccountSummaries.AddAsync(summary);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "AccountSummary created | AccountId: {AccountId} | AccountNumber: {AccountNumber}",
                payload.AccountId, payload.AccountNumber);

            _channel!.BasicAck(
                deliveryTag: args.DeliveryTag,
                multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception processing AccountOpenedEvent | MessageId: {MessageId}",
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

/// <summary>
/// Tolerant Reader — only the fields Transaction Service needs.
/// </summary>
internal sealed record AccountOpenedPayload(
    Guid AccountId,
    Guid CustomerId,
    string AccountNumber,
    string AccountType,
    string Currency,
    decimal InitialBalance,
    decimal DailyTransactionLimit
);