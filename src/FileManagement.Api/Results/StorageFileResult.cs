using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FileManagement.Api.Results;

public sealed class StorageFileResult : IActionResult
{
    private readonly string _contentType;
    private readonly string _fileName;
    private readonly long _sizeBytes;
    private readonly bool _inline;
    private readonly Func<
        Stream,
        CancellationToken,
        Task> _writeAsync;

    public StorageFileResult(
        string contentType,
        string fileName,
        long sizeBytes,
        bool inline,
        Func<Stream, CancellationToken, Task> writeAsync)
    {
        _contentType = contentType;
        _fileName = fileName;
        _sizeBytes = sizeBytes;
        _inline = inline;
        _writeAsync = writeAsync;
    }

    public async Task ExecuteResultAsync(
        ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        var response = httpContext.Response;
        var cancellationToken =
            httpContext.RequestAborted;

        response.ContentType = _contentType;
        response.ContentLength = _sizeBytes;

        var contentDisposition =
            new ContentDispositionHeaderValue(
                _inline
                    ? "inline"
                    : "attachment");

        contentDisposition.SetHttpFileName(
            _fileName);

        response.GetTypedHeaders()
            .ContentDisposition = contentDisposition;

        await response.StartAsync(
            cancellationToken);

        await _writeAsync(
            response.Body,
            cancellationToken);

        await response.Body.FlushAsync(
            cancellationToken);
    }
}