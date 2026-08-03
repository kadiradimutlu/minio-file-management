using System.Text.Json;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;

namespace FileManagement.Reporting.Worker.Reporting;

public sealed class FileOperationEventParser
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    public bool TryParse(
        string payload,
        out FileOperationOccurredV1? operation)
    {
        operation = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var envelope =
                JsonSerializer.Deserialize<
                    IntegrationEventEnvelope<
                        FileOperationOccurredV1>>(
                    payload,
                    SerializerOptions);

            if (
                envelope is null ||
                !envelope.EventType.Equals(
                    FileOperationOccurredV1
                        .EventType,
                    StringComparison.Ordinal) ||
                envelope.EventVersion !=
                    FileOperationOccurredV1
                        .EventVersion ||
                !envelope.Producer.Equals(
                    FileOperationOccurredV1
                        .Producer,
                    StringComparison.Ordinal)
            )
            {
                return false;
            }

            operation = envelope.Payload;

            return true;
        }
        catch (
            Exception exception
        ) when (
            exception is
                JsonException or
                ArgumentException or
                NotSupportedException
        )
        {
            return false;
        }
    }
}
