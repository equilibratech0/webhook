using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Messaging.Abstractions;
using Webhook.Application.Interfaces;
using Webhook.Domain.Events;

namespace Webhook.Infrastructure.Messaging;

public class TransactionPublisher : ITransactionPublisher
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<TransactionPublisher> _logger;

    public TransactionPublisher(IMessagePublisher messagePublisher, ILogger<TransactionPublisher> logger)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(TransactionIngestionCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing TransactionIngestionCreatedEvent for IdempotencyKey: {IdempotencyKey}", domainEvent.IdempotencyKey);

        await _messagePublisher.PublishIntegrationEventAsync(domainEvent, cancellationToken);
    }
}
