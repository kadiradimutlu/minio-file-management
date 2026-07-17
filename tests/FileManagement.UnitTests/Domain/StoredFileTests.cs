using FileManagement.Domain.Entities;

namespace FileManagement.UnitTests.Domain;

public sealed class StoredFileTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesStoredFile()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var storedFile = new StoredFile(
            "report.pdf",
            "2026/07/generated-name.pdf",
            "files",
            "application/pdf",
            2048);

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, storedFile.Id);
        Assert.Equal("report.pdf", storedFile.OriginalFileName);
        Assert.Equal(
            "2026/07/generated-name.pdf",
            storedFile.ObjectName);
        Assert.Equal("files", storedFile.BucketName);
        Assert.Equal("application/pdf", storedFile.ContentType);
        Assert.Equal(2048, storedFile.SizeBytes);

        Assert.InRange(
            storedFile.CreatedAtUtc,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void Constructor_WithEmptyFileName_ThrowsException()
    {
        var action = () => new StoredFile(
            "",
            "object.pdf",
            "files",
            "application/pdf",
            100);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithNegativeSize_ThrowsException()
    {
        var action = () => new StoredFile(
            "report.pdf",
            "object.pdf",
            "files",
            "application/pdf",
            -1);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}