using System.Text.Json;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;
using FileManagement.Operations.Worker.Messaging;
using Xunit;

namespace FileManagement.Operations.UnitTests.Messaging;

public sealed class FileOperationEventDeserializerTests
{
    private readonly FileOperationEventDeserializer _deserializer =
        new();

    [Fact]
    public void Deserialize_WithValidEnvelope_ReturnsContract()
    {
        var envelope =
            CreateEnvelope();

        var json =
            JsonSerializer.Serialize(
                envelope);

        var result =
            _deserializer.Deserialize(
                json);

        Assert.Equal(
            envelope.EventId,
            result.EventId);

        Assert.Equal(
            FileOperationOccurredV1.EventType,
            result.EventType);

        Assert.Equal(
            FileOperationKinds.Uploaded,
            result.Payload.Operation);

        Assert.Equal(
            envelope.Payload.FileId,
            result.Payload.FileId);
    }

    [Fact]
    public void Deserialize_WithUnsupportedEventType_ThrowsException()
    {
        var envelope =
            CreateEnvelope();

        var json =
            JsonSerializer.Serialize(
                envelope);

        var invalidJson =
            json.Replace(
                FileOperationOccurredV1.EventType,
                "file.operation.unknown.v1",
                StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                _deserializer.Deserialize(
                    invalidJson));
    }

    [Fact]
    public void Deserialize_WithMalformedJson_ThrowsException()
    {
        Assert.Throws<JsonException>(
            () =>
                _deserializer.Deserialize(
                    "{ invalid json }"));
    }

    private static IntegrationEventEnvelope<
        FileOperationOccurredV1> CreateEnvelope()
    {
        var payload =
            new FileOperationOccurredV1(
                Guid.NewGuid(),
                FileOperationKinds.Uploaded,
                "report.pdf",
                "application/pdf",
                2048,
                "Student",
                "42",
                "user-123");

        return new IntegrationEventEnvelope<
            FileOperationOccurredV1>(
            Guid.NewGuid(),
            FileOperationOccurredV1.EventType,
            FileOperationOccurredV1.EventVersion,
            DateTimeOffset.UtcNow,
            FileOperationOccurredV1.Producer,
            "correlation-123",
            payload);
    }
}
