using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Entities;

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
        var clientsSection = configuration.GetSection("WebhookAuth:Clients");

        IConfigurationSection? matchedClient = null;

        foreach (var client in clientsSection.GetChildren())
        {
            var apiKey = client.GetSection("ApiKey").Value;
            if (apiKey == extractedApiKey)
            {
                matchedClient = client;
                break;
            }
        }

        if (matchedClient is null)
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Invalid client key." });
            return;
        }

        var clientIdStr = matchedClient.GetSection("ClientId").Value;

        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Client ID configuration is missing or invalid." });
            return;
        }

        var clientName = matchedClient.GetSection("ClientName").Value ?? string.Empty;
        var userIds = matchedClient.GetSection("UserIds").GetChildren()
            .Select(x => x.Value!)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        context.HttpContext.Items["ClientContext"] = new ClientContext(clientId, clientName, userIds);
    }
}
