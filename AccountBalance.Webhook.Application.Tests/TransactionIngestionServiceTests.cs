using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Domain.Events;
using AccountBalance.Webhook.Application.Interfaces;
using AccountBalance.Webhook.Application.Services;

namespace AccountBalance.Webhook.Application.Tests;

public class TransactionIngestionServiceTests
{
    private readonly Mock<IIngestionRepository> _mockRepository;
    private readonly Mock<ITransactionPublisher> _mockPublisher;
    private readonly Mock<ILogger<TransactionIngestionService>> _mockLogger;
    private readonly TransactionIngestionService _sut;

    public TransactionIngestionServiceTests()
    {
        _mockRepository = new Mock<IIngestionRepository>();
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<TransactionIngestionModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockPublisher = new Mock<ITransactionPublisher>();
        _mockLogger = new Mock<ILogger<TransactionIngestionService>>();

        _sut = new TransactionIngestionService(
            _mockRepository.Object,
            _mockPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task IngestAsync_WhenIdempotencyKeyIsEmpty_ReturnsFailure()
    {
        var result = await _sut.IngestAsync(Guid.NewGuid(), string.Empty, MovementEventType.TransactionApproved, "{}");

        Assert.False(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        Assert.Equal("IdempotencyKey is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task IngestAsync_WhenTransactionIsDuplicate_ReturnsDuplicate()
    {
        var companyId = Guid.NewGuid();
        var key = "existing-key";
        var compositeKey = $"{companyId}:{key}";

        _mockRepository.Setup(r => r.ExistsAsync(compositeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.IngestAsync(companyId, key, MovementEventType.TransactionApproved, "{}");

        Assert.True(result.IsSuccess);
        Assert.True(result.IsDuplicate);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TransactionReceivedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenValidNewTransaction_PublishesEventAndReturnsSuccess()
    {
        var key = "new-key";
        var companyId = Guid.NewGuid();
        var compositeKey = $"{companyId}:{key}";
        var rawPayload = "{\"Amount\":100}";

        _mockRepository.Setup(r => r.ExistsAsync(compositeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.IngestAsync(companyId, key, MovementEventType.TransactionApproved, rawPayload);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        _mockRepository.Verify(r => r.SaveAsync(
            It.Is<TransactionIngestionModel>(m => m.IdempotencyKey == compositeKey),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.PublishAsync(
            It.Is<TransactionReceivedEvent>(e =>
                e.CompanyId == companyId &&
                e.RawPayload == rawPayload),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_SameKeyDifferentCompany_ProducesDifferentCompositeKeys()
    {
        var key = "shared-key";
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var rawPayload = "{\"Amount\":50}";

        _mockRepository.Setup(r => r.ExistsAsync($"{companyA}:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.ExistsAsync($"{companyB}:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultA = await _sut.IngestAsync(companyA, key, MovementEventType.TransactionApproved, rawPayload);
        var resultB = await _sut.IngestAsync(companyB, key, MovementEventType.TransactionApproved, rawPayload);

        Assert.True(resultA.IsSuccess);
        Assert.False(resultA.IsDuplicate);
        Assert.True(resultB.IsSuccess);
        Assert.False(resultB.IsDuplicate);
    }
}
