using FileManagement.Api.Models;
using FileManagement.Api.Options;
using FileManagement.Api.Results;
using FileManagement.Application.Files;
using FileManagement.Application.Files.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private const int MaximumPresignedMinutes =
        7 * 24 * 60;

    private readonly IFileManagementService _fileService;
    private readonly FileUploadOptions _uploadOptions;

    public FilesController(
        IFileManagementService fileService,
        IOptions<FileUploadOptions> uploadOptions)
    {
        _fileService = fileService;
        _uploadOptions = uploadOptions.Value;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        typeof(StoredFileDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(
        StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<StoredFileDto>> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File;

        if (file.Length <= 0)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title: "The uploaded file is empty.");
        }

        if (file.Length >
            _uploadOptions.MaxFileSizeBytes)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status413PayloadTooLarge,
                title:
                    "The uploaded file exceeds the configured size limit.");
        }

        var extension = Path.GetExtension(
            file.FileName);

        if (!_uploadOptions.AllowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status415UnsupportedMediaType,
                title:
                    "The uploaded file extension is not supported.");
        }

        var contentType = file.ContentType
            .Split(';', 2)[0]
            .Trim();

        if (!_uploadOptions.AllowedContentTypes.Contains(
            contentType,
            StringComparer.OrdinalIgnoreCase))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status415UnsupportedMediaType,
                title:
                    "The uploaded content type is not supported.");
        }

        await using var content =
            file.OpenReadStream();

        var storedFile =
            await _fileService.UploadAsync(
                file.FileName,
                contentType,
                file.Length,
                content,
                request.RelatedRecordType,
                request.RelatedRecordId,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = storedFile.Id
            },
            storedFile);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<StoredFileDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<
        ActionResult<IReadOnlyList<StoredFileDto>>> List(
        [FromQuery] ListFilesQuery query,
        CancellationToken cancellationToken)
    {
        var files = await _fileService.ListAsync(
            query.RelatedRecordType,
            query.RelatedRecordId,
            cancellationToken);

        return Ok(files);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(StoredFileDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoredFileDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await _fileService.GetByIdAsync(
            id,
            cancellationToken);

        return file is null
            ? NotFound()
            : Ok(file);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await _fileService.GetByIdAsync(
            id,
            cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        return CreateStorageResult(
            id,
            file,
            inline: false);
    }

    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Preview(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await _fileService.GetByIdAsync(
            id,
            cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        if (!SupportsPreview(file.ContentType))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status415UnsupportedMediaType,
                title:
                    "Preview is not supported for this file type.");
        }

        return CreateStorageResult(
            id,
            file,
            inline: true);
    }

    [HttpGet("{id:guid}/presigned-url")]
    [ProducesResponseType(
        typeof(FileAccessUrlDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileAccessUrlDto>>
        CreatePresignedUrl(
            Guid id,
            [FromQuery] int expiresInMinutes = 5,
            CancellationToken cancellationToken = default)
    {
        if (
            expiresInMinutes < 1 ||
            expiresInMinutes >
                MaximumPresignedMinutes
        )
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Expiry must be between 1 and 10080 minutes.");
        }

        var result =
            await _fileService
                .CreatePresignedGetUrlAsync(
                    id,
                    TimeSpan.FromMinutes(
                        expiresInMinutes),
                    cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _fileService.DeleteAsync(
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }

    private StorageFileResult CreateStorageResult(
        Guid id,
        StoredFileDto file,
        bool inline)
    {
        return new StorageFileResult(
            file.ContentType,
            file.OriginalFileName,
            file.SizeBytes,
            inline,
            async (
                destination,
                cancellationToken) =>
            {
                var streamedFile =
                    inline
                        ? await _fileService.PreviewAsync(
                            id,
                            destination,
                            cancellationToken)
                        : await _fileService.DownloadAsync(
                            id,
                            destination,
                            cancellationToken);

                if (streamedFile is null)
                {
                    throw new InvalidOperationException(
                        "The file metadata disappeared during streaming.");
                }
            });
    }

    private static bool SupportsPreview(
        string contentType)
    {
        return contentType.Equals(
                "application/pdf",
                StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase);
    }
}
