using System.Threading;
using System.Threading.Tasks;
using Shared.Domain.Entities;
using Shared.Domain.Events;

namespace AccountBalance.Webhook.Application.Interfaces;

public interface IIngestionRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface ITransactionPublisher
{
    // Publish events (e.g., TransactionReceivedEvent)
    Task PublishAsync(TransactionReceivedEvent @event, CancellationToken cancellationToken = default);
}
