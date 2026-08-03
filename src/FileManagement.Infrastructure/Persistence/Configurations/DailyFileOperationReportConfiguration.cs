using FileManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManagement.Infrastructure.Persistence.Configurations;

public sealed class DailyFileOperationReportConfiguration :
    IEntityTypeConfiguration<DailyFileOperationReport>
{
    public void Configure(
        EntityTypeBuilder<DailyFileOperationReport> builder)
    {
        builder.ToTable(
            "daily_file_operation_reports");

        builder.HasKey(
            report => report.ReportDate);

        builder.Property(
                report => report.ReportDate)
            .HasColumnName("report_date")
            .HasColumnType("date")
            .ValueGeneratedNever();

        builder.Property(
                report => report.UploadedCount)
            .HasColumnName("uploaded_count")
            .IsRequired();

        builder.Property(
                report => report.DownloadedCount)
            .HasColumnName("downloaded_count")
            .IsRequired();

        builder.Property(
                report => report.DeletedCount)
            .HasColumnName("deleted_count")
            .IsRequired();

        builder.Property(
                report => report.UploadedBytes)
            .HasColumnName("uploaded_bytes")
            .IsRequired();

        builder.Property(
                report => report.DownloadedBytes)
            .HasColumnName("downloaded_bytes")
            .IsRequired();

        builder.Property(
                report => report.PendingOutboxCount)
            .HasColumnName("pending_outbox_count")
            .IsRequired();

        builder.Property(
                report => report.FailedOutboxCount)
            .HasColumnName("failed_outbox_count")
            .IsRequired();

        builder.Property(
                report => report.InvalidEventCount)
            .HasColumnName("invalid_event_count")
            .IsRequired();

        builder.Property(
                report =>
                    report.ContentTypeBreakdownJson)
            .HasColumnName(
                "content_type_breakdown")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(
                report => report.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType(
                "timestamp with time zone")
            .IsRequired();

        builder.Property(
                report => report.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType(
                "timestamp with time zone")
            .IsRequired();
    }
}
