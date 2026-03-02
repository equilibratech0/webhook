using System;
using Shared.Domain.Enums;

namespace Webhook.Domain.Models;

public class TransactionIngestionModel
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; }
    public MovementEventType EventType { get; private set; }
    public string RawPayload { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    private TransactionIngestionModel() { } // For EF/Serialization

    public TransactionIngestionModel(string idempotencyKey, MovementEventType eventType, string rawPayload)
    {
        Id = Guid.NewGuid();
        IdempotencyKey = idempotencyKey ?? throw new ArgumentNullException(nameof(idempotencyKey));
        EventType = eventType;
        RawPayload = rawPayload ?? throw new ArgumentNullException(nameof(rawPayload));
        ReceivedAt = DateTime.UtcNow;
    }
}
