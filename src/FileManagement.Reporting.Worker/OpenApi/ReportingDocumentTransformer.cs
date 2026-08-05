using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FileManagement.Reporting.Worker.OpenApi;

public sealed class ReportingDocumentTransformer :
    IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title =
            "File Management Reporting API";
        document.Info.Description =
            "Daily file-operation reports and idempotent report job scheduling.";
        document.Info.Version =
            "v1";

        return Task.CompletedTask;
    }
}
