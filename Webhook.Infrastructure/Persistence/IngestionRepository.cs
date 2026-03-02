using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Infrastructure.Persistence.Abstractions;
using Webhook.Application.Interfaces;
using Webhook.Domain.Models;

namespace Webhook.Infrastructure.Persistence;

public class IngestionRepository : IIngestionRepository
{
    private readonly IMongoCollection<TransactionIngestionModel> _collection;
    private readonly ILogger<IngestionRepository> _logger;

    public IngestionRepository(IMongoDbContext dbContext, ILogger<IngestionRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collection = dbContext.GetCollection<TransactionIngestionModel>("TransactionIngestions");
    }

    public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of IdempotencyKey: {IdempotencyKey}", idempotencyKey);

        var filter = Builders<TransactionIngestionModel>.Filter.Eq(x => x.IdempotencyKey, idempotencyKey);

        // We only care if it exists or not
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
}
