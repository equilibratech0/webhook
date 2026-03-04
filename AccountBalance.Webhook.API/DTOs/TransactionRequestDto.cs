using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class TransactionRequestDto
{
    [Required]
    public MovementEventType EventType { get; set; }

    [Required]
    public MovementPayloadDto Movement { get; set; } = null!;
}
