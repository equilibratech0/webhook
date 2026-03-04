using System.ComponentModel.DataAnnotations;

namespace AccountBalance.Webhook.API.DTOs;

public class MovementPayloadDto
{
    [Required]
    public AmountDto Amount { get; set; } = null!;

    [Required]
    public string TransactionId { get; set; } = null!;

    public string? AccountId { get; set; }

    public string? Country { get; set; }

    public PaymentMethodDto? PaymentMethod { get; set; }

    public MerchantDto? Merchant { get; set; }

    public string? Description { get; set; }
}
