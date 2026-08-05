using FileManagement.Operations.Worker.Messaging;
using FileManagement.Operations.Worker.Options;
using Serilog;

var builder =
    Host.CreateApplicationBuilder(args);

var loggerConfiguration =
    new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty(
            "Application",
            "FileManagement.Operations.Worker")
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

builder.Services
    .AddOptions<KafkaConsumerOptions>()
    .Bind(
        builder.Configuration.GetSection(
            KafkaConsumerOptions.SectionName))
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
                options.GroupId),
        "Kafka GroupId is required.")
    .Validate(
        static options =>
            !string.IsNullOrWhiteSpace(
                options.ClientId),
        "Kafka ClientId is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    FileOperationEventDeserializer>();

builder.Services.AddSingleton<
    IFileOperationEventHandler,
    LoggingFileOperationEventHandler>();

builder.Services.AddHostedService<
    KafkaFileOperationConsumer>();

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
        "Operations Worker terminated unexpectedly.");

    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
