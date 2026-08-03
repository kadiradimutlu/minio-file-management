using System.Text;
using FileManagement.Reporting.Worker.Options;
using FileManagement.Reporting.Worker.Security;
using Microsoft.Extensions.Options;

namespace FileManagement.Reporting.UnitTests.Security;

public sealed class
    ReportingDashboardCredentialValidatorTests
{
    private const string Username =
        "reporting-admin";

    private const string Password =
        "a-strong-reporting-password";

    private readonly
        ReportingDashboardCredentialValidator
        _validator =
            new(
                Options.Create(
                    new ReportingDashboardOptions
                    {
                        Username = Username,
                        Password = Password
                    }));

    [Fact]
    public void IsValid_WithCorrectCredentials_ReturnsTrue()
    {
        var header =
            CreateHeader(
                Username,
                Password);

        Assert.True(
            _validator.IsValid(
                header));
    }

    [Fact]
    public void IsValid_WithWrongPassword_ReturnsFalse()
    {
        var header =
            CreateHeader(
                Username,
                "wrong-password");

        Assert.False(
            _validator.IsValid(
                header));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer token")]
    [InlineData("Basic invalid-base64")]
    public void IsValid_WithMalformedHeader_ReturnsFalse(
        string? header)
    {
        Assert.False(
            _validator.IsValid(
                header));
    }

    private static string CreateHeader(
        string username,
        string password)
    {
        var credentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{username}:{password}"));

        return $"Basic {credentials}";
    }
}
