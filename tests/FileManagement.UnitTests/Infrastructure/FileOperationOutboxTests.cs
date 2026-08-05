using System.Text.Json;
using FileManagement.Contracts.Files;
using FileManagement.Domain.Entities;
using FileManagement.Infrastructure.Persistence;
using FileManagement.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FileManagement.UnitTests.Infrastructure;

public sealed class FileOperationOutboxTests
{
    [Fact]
    public async Task EnqueueAsync_WithValidOperation_TracksSerializedMessage()
    {
        await using var dbContext =
            CreateDbContext();

        var outbox = new FileOperationOutbox(
            dbContext);

        var storedFile = new StoredFile(
            "report.pdf",
            "2026/07/report.pdf",
            "files",
            "application/pdf",
            128,
            "Student",
            "42");

        var occurredAt =
            new DateTimeOffset(
                2026,
                7,
                30,
                14,
                0,
                0,
                TimeSpan.FromHours(3));

        await outbox.EnqueueAsync(
            storedFile,
            " UPLOADED ",
            " user-42 ",
            " correlation-123 ",
            occurredAt);

        var message = Assert.Single(
            dbContext.OutboxMessages.Local);

        Assert.NotEqual(
            Guid.Empty,
            message.Id);

        Assert.Equal(
            FileOperationOccurredV1.EventType,
            message.EventType);

        Assert.Equal(
            FileOperationOccurredV1.EventVersion,
            message.EventVersion);

        Assert.Equal(
            FileOperationOccurredV1.Producer,
            message.Producer);

        Assert.Equal(
            "correlation-123",
            message.CorrelationId);

        Assert.Equal(
            occurredAt.ToUniversalTime(),
            message.OccurredAtUtc);

        Assert.Null(
            message.ProcessedAtUtc);

        Assert.Equal(
            0,
            message.RetryCount);

        using var document =
            JsonDocument.Parse(
                message.Payload);

        var root = document.RootElement;

        Assert.Equal(
            message.Id,
            root.GetProperty("eventId").GetGuid());

        Assert.Equal(
            FileOperationOccurredV1.EventType,
            root.GetProperty("eventType").GetString());

        Assert.Equal(
            FileOperationOccurredV1.EventVersion,
            root.GetProperty("eventVersion").GetInt32());

        Assert.Equal(
            FileOperationOccurredV1.Producer,
            root.GetProperty("producer").GetString());

        Assert.Equal(
            "correlation-123",
            root.GetProperty("correlationId").GetString());

        var payload =
            root.GetProperty("payload");

        Assert.Equal(
            storedFile.Id,
            payload.GetProperty("fileId").GetGuid());

        Assert.Equal(
            FileOperationKinds.Uploaded,
            payload.GetProperty("operation").GetString());

        Assert.Equal(
            "report.pdf",
            payload.GetProperty("originalFileName").GetString());

        Assert.Equal(
            "application/pdf",
            payload.GetProperty("contentType").GetString());

        Assert.Equal(
            128,
            payload.GetProperty("sizeBytes").GetInt64());

        Assert.Equal(
            "Student",
            payload.GetProperty("relatedRecordType").GetString());

        Assert.Equal(
            "42",
            payload.GetProperty("relatedRecordId").GetString());

        Assert.Equal(
            "user-42",
            payload.GetProperty("actorUserId").GetString());
    }

    [Fact]
    public async Task EnqueueAsync_WithUnsupportedOperation_DoesNotTrackMessage()
    {
        await using var dbContext =
            CreateDbContext();

        var outbox = new FileOperationOutbox(
            dbContext);

        var storedFile = new StoredFile(
            "report.pdf",
            "2026/07/report.pdf",
            "files",
            "application/pdf",
            128);

        await Assert.ThrowsAsync<
            ArgumentOutOfRangeException>(
                () => outbox.EnqueueAsync(
                    storedFile,
                    "renamed",
                    "user-42",
                    "correlation-123",
                    DateTimeOffset.UtcNow));

        Assert.Empty(
            dbContext.OutboxMessages.Local);
    }

    private static FileManagementDbContext
        CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<
                FileManagementDbContext>()
                .UseNpgsql(
                    "Host=127.0.0.1;" +
                    "Database=file_management_tests;" +
                    "Username=test;" +
                    "Password=test")
                .Options;

        return new FileManagementDbContext(
            options);
    }
}
