using System.Text.Json;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;
using FileManagement.Reporting.Worker.Reporting;

namespace FileManagement.Reporting.UnitTests.Reporting;

public sealed class FileOperationEventParserTests
{
    private readonly FileOperationEventParser
        _parser = new();

    [Fact]
    public void TryParse_WithValidEnvelope_ReturnsPayload()
    {
        var payload =
            CreatePayload(
                FileOperationKinds.Uploaded);

        var envelope =
            new IntegrationEventEnvelope<
                FileOperationOccurredV1>(
                Guid.NewGuid(),
                FileOperationOccurredV1.EventType,
                FileOperationOccurredV1.EventVersion,
                DateTimeOffset.UtcNow,
                FileOperationOccurredV1.Producer,
                "correlation-123",
                payload);

        var result =
            _parser.TryParse(
                JsonSerializer.Serialize(
                    envelope),
                out var parsed);

        Assert.True(result);
        Assert.NotNull(parsed);
        Assert.Equal(
            payload.FileId,
            parsed.FileId);
    }

    [Fact]
    public void TryParse_WithMalformedJson_ReturnsFalse()
    {
        var result =
            _parser.TryParse(
                "{invalid",
                out var parsed);

        Assert.False(result);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParse_WithUnexpectedEnvelopeType_ReturnsFalse()
    {
        var payload =
            CreatePayload(
                FileOperationKinds.Downloaded);

        var envelope =
            new IntegrationEventEnvelope<
                FileOperationOccurredV1>(
                Guid.NewGuid(),
                "file.operation.occurred.v2",
                FileOperationOccurredV1.EventVersion,
                DateTimeOffset.UtcNow,
                FileOperationOccurredV1.Producer,
                "correlation-123",
                payload);

        var result =
            _parser.TryParse(
                JsonSerializer.Serialize(
                    envelope),
                out var parsed);

        Assert.False(result);
        Assert.Null(parsed);
    }

    private static FileOperationOccurredV1
        CreatePayload(
            string operation)
    {
        return new FileOperationOccurredV1(
            Guid.NewGuid(),
            operation,
            "report.pdf",
            "application/pdf",
            2048,
            null,
            null,
            "user-123");
    }
}
