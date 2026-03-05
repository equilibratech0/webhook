using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
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
        // Act
        var result = await _sut.IngestAsync(Guid.NewGuid(), "TestClient", string.Empty, MovementEventType.TransactionCreated, "{}");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        Assert.Equal("IdempotencyKey is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task IngestAsync_WhenTransactionIsDuplicate_ReturnsDuplicate()
    {
        // Arrange
        var key = "existing-key";
        _mockRepository.Setup(r => r.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IngestAsync(Guid.NewGuid(), "TestClient", key, MovementEventType.TransactionCreated, "{}");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsDuplicate);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TransactionReceivedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenValidNewTransaction_PublishesEventAndReturnsSuccess()
    {
        // Arrange
        var key = "new-key";
        var clientId = Guid.NewGuid();
        var rawPayload = "{\"Amount\":100}";

        _mockRepository.Setup(r => r.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.IngestAsync(clientId, "TestClient", key, MovementEventType.TransactionCreated, rawPayload);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        _mockRepository.Verify(r => r.SaveAsync(It.Is<TransactionIngestionModel>(m => m.IdempotencyKey == key), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.PublishAsync(
            It.Is<TransactionReceivedEvent>(e =>
                e.IdempotencyKey == key &&
                e.ClientId == clientId &&
                e.RawPayload == rawPayload),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
