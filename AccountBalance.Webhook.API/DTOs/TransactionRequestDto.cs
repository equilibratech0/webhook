using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class TransactionRequestDto
{
    [Required]
    public MovementEventType EventType { get; set; }

    [Required]
    public MovementPayloadDto Payload { get; set; } = null!;
}

public class MovementPayloadDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    [Required]
    public string ReferenceId { get; set; } = null!;

    [Required]
    public string AccountId { get; set; } = null!;

    [Required]
    public string Country { get; set; } = null!;

    [Required]
    public PaymentMethodDto PaymentMethod { get; set; } = null!;

    public string? Description { get; set; }
}

public class PaymentMethodDto
{
    [Required]
    public string PaymentMethodId { get; set; } = null!;

    [Required]
    public string PaymentMethodName { get; set; } = null!;

    [Required]
    public PaymentMethodType Type { get; set; }
}
