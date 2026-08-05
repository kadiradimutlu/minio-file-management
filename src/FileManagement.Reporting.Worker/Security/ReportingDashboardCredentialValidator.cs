using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using FileManagement.Reporting.Worker.Options;
using Microsoft.Extensions.Options;

namespace FileManagement.Reporting.Worker.Security;

public sealed class ReportingDashboardCredentialValidator(
    IOptions<ReportingDashboardOptions> options)
{
    private readonly ReportingDashboardOptions
        _options = options.Value;

    public bool IsValid(
        string? authorizationHeader)
    {
        if (
            !AuthenticationHeaderValue.TryParse(
                authorizationHeader,
                out var header) ||
            !header.Scheme.Equals(
                "Basic",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(
                header.Parameter)
        )
        {
            return false;
        }

        string decoded;

        try
        {
            decoded =
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        header.Parameter));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex =
            decoded.IndexOf(':');

        if (separatorIndex <= 0)
        {
            return false;
        }

        var username =
            decoded[..separatorIndex];
        var password =
            decoded[(separatorIndex + 1)..];

        var usernameMatches =
            FixedTimeEquals(
                username,
                _options.Username);

        var passwordMatches =
            FixedTimeEquals(
                password,
                _options.Password);

        return usernameMatches &
            passwordMatches;
    }

    private static bool FixedTimeEquals(
        string provided,
        string expected)
    {
        var providedHash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    provided));

        var expectedHash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    expected));

        return CryptographicOperations
            .FixedTimeEquals(
                providedHash,
                expectedHash);
    }
}
