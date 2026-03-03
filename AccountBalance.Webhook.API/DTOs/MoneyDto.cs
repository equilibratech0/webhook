using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace AccountBalance.Webhook.API.DTOs;

public class MoneyDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }
}
