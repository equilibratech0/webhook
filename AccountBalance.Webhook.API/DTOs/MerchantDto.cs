namespace AccountBalance.Webhook.API.DTOs;

public class MerchantDto
{
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public ShopDto? Shop { get; set; }
}

public class ShopDto
{
    public string? ShopId { get; set; }
    public string? ShopName { get; set; }
}
