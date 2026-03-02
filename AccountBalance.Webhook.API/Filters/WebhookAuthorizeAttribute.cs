using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountBalance.Webhook.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WebhookAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-Client-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Client key is missing." });
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredApiKey = configuration.GetValue<string>("WebhookAuth:ClientKey");

        if (string.IsNullOrEmpty(configuredApiKey) || !configuredApiKey.Equals(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Invalid client key." });
        }
    }
}
