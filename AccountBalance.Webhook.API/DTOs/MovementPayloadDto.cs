using System.ComponentModel.DataAnnotations;

namespace AccountBalance.Webhook.API.DTOs;

public class MovementPayloadDto
{
    [Required]
    public MoneyDto Amount { get; set; } = null!;

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
