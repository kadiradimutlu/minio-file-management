using System.Text.Json;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;

namespace FileManagement.Operations.Worker.Messaging;

public sealed class FileOperationEventDeserializer
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNameCaseInsensitive = false
        };

    public IntegrationEventEnvelope<FileOperationOccurredV1> Deserialize(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new JsonException(
                "Kafka message value cannot be empty.");
        }

        var envelope =
            JsonSerializer.Deserialize<
                IntegrationEventEnvelope<
                    FileOperationOccurredV1>>(
                json,
                SerializerOptions);

        if (envelope is null)
        {
            throw new JsonException(
                "Kafka message could not be deserialized.");
        }

        if (
            !string.Equals(
                envelope.EventType,
                FileOperationOccurredV1.EventType,
                StringComparison.Ordinal)
        )
        {
            throw new InvalidDataException(
                $"Unsupported event type: {envelope.EventType}");
        }

        if (
            envelope.EventVersion !=
            FileOperationOccurredV1.EventVersion
        )
        {
            throw new InvalidDataException(
                $"Unsupported event version: {envelope.EventVersion}");
        }

        if (
            !string.Equals(
                envelope.Producer,
                FileOperationOccurredV1.Producer,
                StringComparison.Ordinal)
        )
        {
            throw new InvalidDataException(
                $"Unsupported event producer: {envelope.Producer}");
        }

        return envelope;
    }
}
