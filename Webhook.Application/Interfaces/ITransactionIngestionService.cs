using System.Threading;
using System.Threading.Tasks;
using Shared.Domain.Enums;

namespace Webhook.Application.Interfaces;

public interface ITransactionIngestionService
{
    Task<IngestionResult> IngestAsync(string idempotencyKey, MovementEventType eventType, string rawPayload, CancellationToken cancellationToken = default);
}

public class IngestionResult
{
    public bool IsSuccess { get; }
    public bool IsDuplicate { get; }
    public string ErrorMessage { get; }

    private IngestionResult(bool isSuccess, bool isDuplicate, string errorMessage)
    {
        IsSuccess = isSuccess;
        IsDuplicate = isDuplicate;
        ErrorMessage = errorMessage;
    }

    public static IngestionResult Success() => new(true, false, string.Empty);
    public static IngestionResult Duplicate() => new(true, true, string.Empty);
    public static IngestionResult Failure(string errorMessage) => new(false, false, errorMessage);
}
