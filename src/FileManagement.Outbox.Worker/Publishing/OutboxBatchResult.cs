namespace FileManagement.Outbox.Worker.Publishing;

public sealed record OutboxBatchResult(
    int SelectedCount,
    int PublishedCount,
    int FailedCount);