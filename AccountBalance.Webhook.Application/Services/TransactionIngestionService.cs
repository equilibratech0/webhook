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
        Guid companyId,
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

            var compositeKey = $"{companyId}:{idempotencyKey}";

            bool exists = await _repository.ExistsAsync(compositeKey, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Duplicate transaction detected for CompanyId: {CompanyId}, IdempotencyKey: {IdempotencyKey}",
                    companyId, idempotencyKey);
                return IngestionResult.Duplicate();
            }

            var model = new TransactionIngestionModel(compositeKey);
            await _repository.SaveAsync(model, cancellationToken);

            var domainEvent = new TransactionReceivedEvent(
                model.Id,
                companyId,
                eventType,
                rawPayload);

            await _publisher.PublishAsync(domainEvent, cancellationToken);

            _logger.LogInformation("Successfully ingested transaction {TransactionId} for CompanyId: {CompanyId}, IdempotencyKey: {IdempotencyKey}",
                model.Id, companyId, idempotencyKey);

            return IngestionResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while ingesting transaction for CompanyId: {CompanyId}, IdempotencyKey: {IdempotencyKey}",
                companyId, idempotencyKey);
            return IngestionResult.Failure("An unexpected error occurred during ingestion.");
        }
    }
}
