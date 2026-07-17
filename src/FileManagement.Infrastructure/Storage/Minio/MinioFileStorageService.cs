using FileManagement.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;
using global::Minio;
using global::Minio.DataModel.Args;
using global::Minio.Exceptions;

namespace FileManagement.Infrastructure.Storage.Minio;

public sealed class MinioFileStorageService :
    IFileStorageService,
    IDisposable
{
    private static readonly TimeSpan MaximumPresignedUrlLifetime =
        TimeSpan.FromDays(7);

    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _options;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);

    private bool _bucketInitialized;

    public string BucketName => _options.BucketName;

    public MinioFileStorageService(
        IMinioClient minioClient,
        IOptions<MinioOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;
    }

    public async Task EnsureBucketExistsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_bucketInitialized)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);

        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_options.BucketName);

            var bucketExists = await _minioClient.BucketExistsAsync(
                bucketExistsArgs,
                cancellationToken);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_options.BucketName);

                await _minioClient.MakeBucketAsync(
                    makeBucketArgs,
                    cancellationToken);
            }

            _bucketInitialized = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    public async Task UploadAsync(
        string objectName,
        Stream content,
        long sizeBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectName(objectName);
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The upload stream must be readable.",
                nameof(content));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                "File size cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException(
                "Content type is required.",
                nameof(contentType));
        }

        await EnsureBucketExistsAsync(cancellationToken);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(sizeBytes)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(
            putObjectArgs,
            cancellationToken);
    }

    public async Task DownloadAsync(
        string objectName,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectName(objectName);
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The destination stream must be writable.",
                nameof(destination));
        }

        await EnsureBucketExistsAsync(cancellationToken);

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithCallbackStream(source =>
            {
                CopyStream(
                    source,
                    destination,
                    cancellationToken);
            });

        await _minioClient.GetObjectAsync(
            getObjectArgs,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectName(objectName);

        await EnsureBucketExistsAsync(cancellationToken);

        var statObjectArgs = new StatObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName);

        try
        {
            await _minioClient.StatObjectAsync(
                statObjectArgs,
                cancellationToken);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectName(objectName);

        await EnsureBucketExistsAsync(cancellationToken);

        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(
            removeObjectArgs,
            cancellationToken);
    }

    public async Task<string> CreatePresignedGetUrlAsync(
        string objectName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectName(objectName);

        if (
            expiresIn <= TimeSpan.Zero ||
            expiresIn > MaximumPresignedUrlLifetime
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresIn),
                "Expiry must be greater than zero and no longer than seven days.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        await EnsureBucketExistsAsync(cancellationToken);

        var expirySeconds = checked(
            (int)Math.Ceiling(expiresIn.TotalSeconds));

        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithExpiry(expirySeconds);

        return await _minioClient.PresignedGetObjectAsync(
            presignedArgs);
    }

    public void Dispose()
    {
        _bucketLock.Dispose();
    }

    private static void ValidateObjectName(
        string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException(
                "Object name is required.",
                nameof(objectName));
        }
    }

    private static void CopyStream(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = source.Read(
                buffer,
                0,
                buffer.Length);

            if (bytesRead == 0)
            {
                break;
            }

            destination.Write(
                buffer,
                0,
                bytesRead);
        }
    }
}