using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AccountBalance.Webhook.API.DTOs;
using AccountBalance.Webhook.Application.Interfaces;

namespace AccountBalance.Webhook.API.Controllers;

[ApiController]
[Route("[controller]")]
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
        [FromHeader(Name = "X-Company-Id")] string companyIdRaw,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] TransactionRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Raw X-Company-Id header value: {CompanyIdRaw}", companyIdRaw);

        if (string.IsNullOrWhiteSpace(companyIdRaw) || !Guid.TryParse(companyIdRaw, out var companyId))
        {
            return BadRequest(new TransactionResponseDto { Success = false, Message = "X-Company-Id header is missing or not a valid GUID." });
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new TransactionResponseDto { Success = false, Message = "Idempotency-Key header is required." });
        }

        string rawPayload = JsonSerializer.Serialize(request.Movement, new JsonSerializerOptions { WriteIndented = false });

        _logger.LogInformation("Received webhook request for CompanyId: {CompanyId}, EventType: {EventType}, IdempotencyKey: {IdempotencyKey}",
            companyId, request.EventType, idempotencyKey);

        var result = await _ingestionService.IngestAsync(companyId, idempotencyKey, request.EventType, rawPayload, cancellationToken);

        if (result.IsSuccess)
        {
            return Accepted(new TransactionResponseDto { Success = true, Message = "Transaction accepted for processing." });
        }

        if (result.IsDuplicate)
        {
            return Ok(new TransactionResponseDto { Success = true, Message = "Transaction already processed." });
        }

        return UnprocessableEntity(new TransactionResponseDto { Success = false, Message = result.ErrorMessage });
    }
}
