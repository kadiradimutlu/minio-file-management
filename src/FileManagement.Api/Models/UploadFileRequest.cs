using System.ComponentModel.DataAnnotations;

namespace FileManagement.Api.Models;

public sealed class UploadFileRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;
}