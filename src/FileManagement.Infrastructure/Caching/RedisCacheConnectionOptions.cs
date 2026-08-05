namespace FileManagement.Infrastructure.Caching;

public sealed class RedisCacheConnectionOptions
{
    public const string SectionName =
        "Redis";

    public string Host { get; set; } =
        string.Empty;

    public int Port { get; set; } =
        6379;

    public string Password { get; set; } =
        string.Empty;

    public bool UseSsl { get; set; }

    public int ConnectTimeoutMilliseconds
    {
        get;
        set;
    } = 1000;

    public int OperationTimeoutMilliseconds
    {
        get;
        set;
    } = 500;
}
