using FileManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManagement.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration :
    IEntityTypeConfiguration<StoredFile>
{
    public void Configure(
        EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");

        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(file => file.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(file => file.ObjectName)
            .HasColumnName("object_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(file => file.BucketName)
            .HasColumnName("bucket_name")
            .HasMaxLength(63)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(file => file.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(file => file.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(
                file => new
                {
                    file.BucketName,
                    file.ObjectName
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_stored_files_bucket_object_name");
    }
}