using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class PaymentMethodDto
{
    [Required]
    public string PaymentMethodId { get; set; } = null!;

    [Required]
    public string PaymentMethodName { get; set; } = null!;

    [Required]
    public PaymentMethodType Type { get; set; }
}
