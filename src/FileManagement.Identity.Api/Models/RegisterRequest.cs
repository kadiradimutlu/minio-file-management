using System.ComponentModel.DataAnnotations;

namespace FileManagement.Identity.Api.Models;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } =
        string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } =
        string.Empty;
}
