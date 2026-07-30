using System.Text.Json;
using FileManagement.Contracts.Events;
using FileManagement.Contracts.Files;

namespace FileManagement.Contracts.UnitTests.Events;

public sealed class FileOperationContractTests
{
    [Fact]
    public void PayloadConstructor_WithValidValues_NormalizesValues()
    {
        var fileId =
            Guid.NewGuid();

        var payload =
            new FileOperationOccurredV1(
                fileId,
                " Uploaded ",
                " report.pdf ",
                " application/pdf ",
                2048,
                " Student ",
                " 42 ",
                " user-123 ");

        Assert.Equal(
            fileId,
            payload.FileId);

        Assert.Equal(
            FileOperationKinds.Uploaded,
            payload.Operation);

        Assert.Equal(
            "report.pdf",
            payload.OriginalFileName);

        Assert.Equal(
            "application/pdf",
            payload.ContentType);

        Assert.Equal(
            2048,
            payload.SizeBytes);

        Assert.Equal(
            "Student",
            payload.RelatedRecordType);

        Assert.Equal(
            "42",
            payload.RelatedRecordId);

        Assert.Equal(
            "user-123",
            payload.ActorUserId);
    }

    [Fact]
    public void PayloadConstructor_WithUnsupportedOperation_ThrowsException()
    {
        var action = () =>
            new FileOperationOccurredV1(
                Guid.NewGuid(),
                "renamed",
                "report.pdf",
                "application/pdf",
                2048,
                null,
                null,
                "user-123");

        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    [Fact]
    public void PayloadConstructor_WithIncompleteAssociation_ThrowsException()
    {
        var action = () =>
            new FileOperationOccurredV1(
                Guid.NewGuid(),
                FileOperationKinds.Uploaded,
                "report.pdf",
                "application/pdf",
                2048,
                "Student",
                null,
                "user-123");

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void Envelope_SerializesAndDeserializesContract()
    {
        var eventId =
            Guid.NewGuid();

        var fileId =
            Guid.NewGuid();

        var occurredAt =
            new DateTimeOffset(
                2026,
                7,
                30,
                10,
                0,
                0,
                TimeSpan.FromHours(3));

        var payload =
            new FileOperationOccurredV1(
                fileId,
                FileOperationKinds.Downloaded,
                "report.pdf",
                "application/pdf",
                2048,
                null,
                null,
                "user-123");

        var envelope =
            new IntegrationEventEnvelope<FileOperationOccurredV1>(
                eventId,
                FileOperationOccurredV1.EventType,
                FileOperationOccurredV1.EventVersion,
                occurredAt,
                FileOperationOccurredV1.Producer,
                "correlation-123",
                payload);

        var json =
            JsonSerializer.Serialize(
                envelope);

        Assert.Contains(
            "\"eventType\":\"file.operation.occurred.v1\"",
            json);

        Assert.Contains(
            "\"operation\":\"downloaded\"",
            json);

        var deserialized =
            JsonSerializer.Deserialize<
                IntegrationEventEnvelope<
                    FileOperationOccurredV1>>(
                json);

        Assert.NotNull(
            deserialized);

        Assert.Equal(
            eventId,
            deserialized.EventId);

        Assert.Equal(
            new DateTimeOffset(
                2026,
                7,
                30,
                7,
                0,
                0,
                TimeSpan.Zero),
            deserialized.OccurredAtUtc);

        Assert.Equal(
            "correlation-123",
            deserialized.CorrelationId);

        Assert.Equal(
            fileId,
            deserialized.Payload.FileId);

        Assert.Equal(
            FileOperationKinds.Downloaded,
            deserialized.Payload.Operation);
    }
}
