using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AccountBalance.Webhook.API.DTOs;
using AccountBalance.Webhook.API.Filters;
using AccountBalance.Webhook.Application.Interfaces;

namespace AccountBalance.Webhook.API.Controllers;

[ApiController]
[Route("[controller]")]
[WebhookAuthorize] // Apply custom authorization filter
public class TransactionsController : ControllerBase
{
    private readonly ITransactionIngestionService _ingestionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionIngestionService ingestionService,
        ILogger<TransactionsController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> IngestTransaction(
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] TransactionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new TransactionResponseDto { Success = false, Message = "Idempotency-Key header is required." });
        }

        if (!HttpContext.Items.TryGetValue("ClientId", out var clientIdObj) || clientIdObj is not Guid clientId)
        {
            _logger.LogError("ClientId not found in HttpContext. Items: {@Items}", HttpContext.Items.Keys);
            return StatusCode(500, new TransactionResponseDto { Success = false, Message = "Internal server error: Client context is missing." });
        }

        string rawPayload = JsonSerializer.Serialize(request.Movement, new JsonSerializerOptions { WriteIndented = false });

        _logger.LogInformation("Received webhook request for ClientId: {ClientId}, EventType: {EventType}, IdempotencyKey: {IdempotencyKey}",
            clientId, request.EventType, idempotencyKey);

        var result = await _ingestionService.IngestAsync(clientId, idempotencyKey, request.EventType, rawPayload, cancellationToken);

        if (result.IsSuccess)
        {
            return Accepted(new TransactionResponseDto { Success = true, Message = "Transaction accepted for processing." });
        }

        if (result.IsDuplicate)
        {
            // Requirement: "If duplicate, returns 200 OK"
            return Ok(new TransactionResponseDto { Success = true, Message = "Transaction already processed." });
        }

        // Catch-all failure
        return UnprocessableEntity(new TransactionResponseDto { Success = false, Message = result.ErrorMessage });
    }
}
