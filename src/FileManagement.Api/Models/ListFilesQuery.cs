using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FileManagement.Api.Models;

public sealed class ListFilesQuery :
    IValidatableObject
{
    [FromQuery(Name = "relatedRecordType")]
    [StringLength(100)]
    public string? RelatedRecordType { get; init; }

    [FromQuery(Name = "relatedRecordId")]
    [StringLength(255)]
    public string? RelatedRecordId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var hasRelatedRecordType =
            !string.IsNullOrWhiteSpace(
                RelatedRecordType);

        var hasRelatedRecordId =
            !string.IsNullOrWhiteSpace(
                RelatedRecordId);

        if (
            hasRelatedRecordType !=
            hasRelatedRecordId
        )
        {
            yield return new ValidationResult(
                "Related record type and related record id must be provided together.",
                [
                    nameof(RelatedRecordType),
                    nameof(RelatedRecordId)
                ]);
        }
    }
}
