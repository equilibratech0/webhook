using System;
using Shared.Domain.Enums;

namespace Webhook.Domain.Events;

public class TransactionIngestionCreatedEvent
{
    public Guid TransactionId { get; }
    public string IdempotencyKey { get; }
    public MovementEventType EventType { get; }
    public string RawPayload { get; }
    public DateTime OccurredOn { get; }

    public TransactionIngestionCreatedEvent(
        Guid transactionId,
        string idempotencyKey,
        MovementEventType eventType,
        string rawPayload)
    {
        TransactionId = transactionId;
        IdempotencyKey = idempotencyKey;
        EventType = eventType;
        RawPayload = rawPayload;
        OccurredOn = DateTime.UtcNow;
    }
}
