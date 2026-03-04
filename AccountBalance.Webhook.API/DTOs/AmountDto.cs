using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class AmountDto
{
    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    public decimal? GrossAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? PaymentFee { get; set; }
}
