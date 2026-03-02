using System.Threading;
using System.Threading.Tasks;
using Webhook.Domain.Models;
using Webhook.Domain.Events;

namespace Webhook.Application.Interfaces;

public interface IIngestionRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface ITransactionPublisher
{
    Task PublishAsync(TransactionIngestionCreatedEvent domainEvent, CancellationToken cancellationToken = default);
}
