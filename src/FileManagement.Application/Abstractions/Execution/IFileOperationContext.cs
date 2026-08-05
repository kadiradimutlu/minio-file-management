namespace FileManagement.Application.Abstractions.Execution;

public interface IFileOperationContext
{
    string ActorUserId { get; }

    string CorrelationId { get; }
}
