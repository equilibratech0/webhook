using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Domain.Enums;
using AccountBalance.Webhook.Application.Interfaces;
using Shared.Domain.Events;
using Shared.Domain.Entities;

namespace AccountBalance.Webhook.Application.Services;

public class TransactionIngestionService : ITransactionIngestionService
{
    private readonly IIngestionRepository _repository;
    private readonly ITransactionPublisher _publisher;
    private readonly ILogger<TransactionIngestionService> _logger;

    public TransactionIngestionService(
        IIngestionRepository repository,
        ITransactionPublisher publisher,
        ILogger<TransactionIngestionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IngestionResult> IngestAsync(
        ClientContext clientContext,
        string idempotencyKey,
        MovementEventType eventType,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _logger.LogWarning("Ingestion failed: IdempotencyKey is missing.");
                return IngestionResult.Failure("IdempotencyKey is required.");
            }

            // 1. Check Idempotency
            bool exists = await _repository.ExistsAsync(idempotencyKey, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Duplicate transaction detected for IdempotencyKey: {IdempotencyKey}", idempotencyKey);
                return IngestionResult.Duplicate();
            }

            // 2. Create Domain Model and Save to DB
            var model = new TransactionIngestionModel(clientContext, idempotencyKey, eventType);
            await _repository.SaveAsync(model, cancellationToken);

            // 3. Create and Publish Event (Service Bus via Infrastructure)
            var domainEvent = new TransactionReceivedEvent(
                model.Id,
                model.ClientId,
                model.ClientName,
                model.UserIds,
                model.IdempotencyKey,
                model.EventType,
                rawPayload);

            await _publisher.PublishAsync(domainEvent, cancellationToken);

            _logger.LogInformation("Successfully ingested transaction {TransactionId} with IdempotencyKey: {IdempotencyKey}",
                model.Id, idempotencyKey);

            return IngestionResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while ingesting transaction with IdempotencyKey: {IdempotencyKey}", idempotencyKey);
            return IngestionResult.Failure("An unexpected error occurred during ingestion.");
        }
    }
}
