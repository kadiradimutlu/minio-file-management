using FileManagement.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManagement.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration :
    IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(
                OutboxMessage.EventTypeMaxLength)
            .IsRequired();

        builder.Property(message => message.EventVersion)
            .HasColumnName("event_version")
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(message => message.Producer)
            .HasColumnName("producer")
            .HasMaxLength(
                OutboxMessage.ProducerMaxLength)
            .IsRequired();

        builder.Property(message => message.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(
                OutboxMessage.CorrelationIdMaxLength)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(
                OutboxMessage.LastErrorMaxLength);

        builder.HasIndex(message => message.CreatedAtUtc)
            .HasDatabaseName(
                "ix_outbox_messages_pending")
            .HasFilter(
                "processed_at_utc IS NULL");
    }
}
