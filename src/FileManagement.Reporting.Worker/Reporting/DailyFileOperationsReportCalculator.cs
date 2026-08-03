using FileManagement.Contracts.Files;

namespace FileManagement.Reporting.Worker.Reporting;

public sealed class DailyFileOperationsReportCalculator
{
    public DailyFileOperationsReportMetrics
        Calculate(
            IEnumerable<
                FileOperationOccurredV1> operations,
            int pendingOutboxCount,
            int failedOutboxCount,
            int invalidEventCount)
    {
        ArgumentNullException.ThrowIfNull(
            operations);

        ValidateCount(
            pendingOutboxCount,
            nameof(pendingOutboxCount));
        ValidateCount(
            failedOutboxCount,
            nameof(failedOutboxCount));
        ValidateCount(
            invalidEventCount,
            nameof(invalidEventCount));

        var uploadedCount = 0;
        var downloadedCount = 0;
        var deletedCount = 0;
        long uploadedBytes = 0;
        long downloadedBytes = 0;

        var contentTypes =
            new SortedDictionary<
                string,
                int>(
                StringComparer.Ordinal);

        foreach (var operation in operations)
        {
            ArgumentNullException.ThrowIfNull(
                operation);

            switch (operation.Operation)
            {
                case FileOperationKinds.Uploaded:
                    uploadedCount++;
                    uploadedBytes =
                        checked(
                            uploadedBytes +
                            operation.SizeBytes);

                    var contentType =
                        operation.ContentType
                            .Trim()
                            .ToLowerInvariant();

                    contentTypes[contentType] =
                        contentTypes
                            .GetValueOrDefault(
                                contentType) +
                        1;
                    break;

                case FileOperationKinds.Downloaded:
                    downloadedCount++;
                    downloadedBytes =
                        checked(
                            downloadedBytes +
                            operation.SizeBytes);
                    break;

                case FileOperationKinds.Deleted:
                    deletedCount++;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported report operation: {operation.Operation}");
            }
        }

        return new DailyFileOperationsReportMetrics(
            uploadedCount,
            downloadedCount,
            deletedCount,
            uploadedBytes,
            downloadedBytes,
            pendingOutboxCount,
            failedOutboxCount,
            invalidEventCount,
            contentTypes);
    }

    private static void ValidateCount(
        int count,
        string parameterName)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Report counts cannot be negative.");
        }
    }
}
