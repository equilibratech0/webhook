using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class PaymentMethodDto
{
    public string? PaymentMethodId { get; set; }

    public string? ProviderName { get; set; }

    public PaymentMethodType? Type { get; set; }
}
