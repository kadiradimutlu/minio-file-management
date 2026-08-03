namespace FileManagement.Application.Abstractions.Caching;

public readonly record struct CacheLookup<T>(
    bool Found,
    T? Value)
    where T : class
{
    public static CacheLookup<T> Miss =>
        new(
            false,
            null);

    public static CacheLookup<T> Hit(
        T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new CacheLookup<T>(
            true,
            value);
    }
}
