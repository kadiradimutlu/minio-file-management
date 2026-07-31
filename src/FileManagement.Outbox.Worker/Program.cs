using FileManagement.Infrastructure.Persistence;
using FileManagement.Outbox.Worker.Messaging;
using FileManagement.Outbox.Worker.Options;
using FileManagement.Outbox.Worker.Publishing;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string ApplicationName =
    "FileManagement.Outbox.Worker";

var builder =
    Host.CreateApplicationBuilder(args);

var loggerConfiguration =
    new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty(
            "Application",
            ApplicationName)
        .Enrich.WithProperty(
            "Environment",
            builder.Environment.EnvironmentName)
        .WriteTo.Console();

var seqServerUrl =
    builder.Configuration[
        "Seq:ServerUrl"];

if (
    !string.IsNullOrWhiteSpace(
        seqServerUrl)
)
{
    loggerConfiguration.WriteTo.Seq(
        seqServerUrl);
}

Log.Logger =
    loggerConfiguration.CreateLogger();

builder.Services.AddSerilog();

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "PostgreSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:PostgreSql is not configured.");
}

builder.Services
    .AddPooledDbContextFactory<
        FileManagementDbContext>(
        options =>
            options.UseNpgsql(
                connectionString));

builder.Services
    .AddOptions<KafkaProducerOptions>()
    .Bind(
        builder.Configuration.GetSection(
            KafkaProducerOptions.SectionName))
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.BootstrapServers),
        "Kafka BootstrapServers is required.")
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.Topic),
        "Kafka Topic is required.")
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.ClientId),
        "Kafka ClientId is required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<OutboxPublisherOptions>()
    .Bind(
        builder.Configuration.GetSection(
            OutboxPublisherOptions.SectionName))
    .Validate(
        static options =>
            options.BatchSize is >= 1 and <= 1000,
        "OutboxPublisher BatchSize must be between 1 and 1000.")
    .Validate(
        static options =>
            options.PollIntervalMilliseconds
                is >= 100 and <= 60000,
        "OutboxPublisher PollIntervalMilliseconds must be between 100 and 60000.")
    .ValidateOnStart();

builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);

builder.Services.AddSingleton<
    IOutboxEventProducer,
    KafkaOutboxEventProducer>();

builder.Services.AddSingleton<
    OutboxMessagePublisher>();

builder.Services.AddSingleton<
    IOutboxBatchProcessor,
    OutboxBatchProcessor>();

builder.Services.AddSingleton<
    OutboxPublisherCycle>();

builder.Services.AddHostedService<
    OutboxPublisherWorker>();

var host =
    builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(
        exception,
        "Outbox Worker terminated unexpectedly.");

    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}