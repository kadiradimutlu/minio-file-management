using FileManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString =
    builder.Configuration.GetConnectionString("PostgreSql");

if (string.IsNullOrWhiteSpace(postgresConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:PostgreSql is not configured.");
}

builder.Services.AddInfrastructure(
    postgresConnectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();