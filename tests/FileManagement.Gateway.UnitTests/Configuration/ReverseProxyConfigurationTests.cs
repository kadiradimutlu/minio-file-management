using System.Text.Json;

namespace FileManagement.Gateway.UnitTests.Configuration;

public sealed class ReverseProxyConfigurationTests
{
    private static readonly JsonDocument
        Configuration =
            JsonDocument.Parse(
                File.ReadAllText(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "gateway-appsettings.json")));

    [Fact]
    public void Routes_ContainExpectedIdentityAndFileBoundaries()
    {
        var routes =
            Configuration.RootElement
                .GetProperty("ReverseProxy")
                .GetProperty("Routes");

        Assert.Equal(
            2,
            routes.EnumerateObject()
                .Count());

        AssertRoute(
            routes,
            "identityRoute",
            "identityCluster",
            "/api/auth/{**catch-all}",
            1_048_576);

        AssertRoute(
            routes,
            "fileRoute",
            "fileCluster",
            "/api/files/{**catch-all}",
            22_020_096);
    }

    [Fact]
    public void Clusters_TargetInternalComposeServices()
    {
        var clusters =
            Configuration.RootElement
                .GetProperty("ReverseProxy")
                .GetProperty("Clusters");

        AssertClusterAddress(
            clusters,
            "identityCluster",
            "identityApi",
            "http://identity-api:8080/");

        AssertClusterAddress(
            clusters,
            "fileCluster",
            "fileApi",
            "http://api:8080/");
    }

    private static void AssertRoute(
        JsonElement routes,
        string routeName,
        string expectedCluster,
        string expectedPath,
        int expectedMaximumBodySize)
    {
        var route =
            routes.GetProperty(
                routeName);

        Assert.Equal(
            expectedCluster,
            route.GetProperty(
                    "ClusterId")
                .GetString());

        Assert.Equal(
            expectedMaximumBodySize,
            route.GetProperty(
                    "MaxRequestBodySize")
                .GetInt32());

        Assert.Equal(
            expectedPath,
            route.GetProperty("Match")
                .GetProperty("Path")
                .GetString());
    }

    private static void
        AssertClusterAddress(
            JsonElement clusters,
            string clusterName,
            string destinationName,
            string expectedAddress)
    {
        var address =
            clusters.GetProperty(
                    clusterName)
                .GetProperty(
                    "Destinations")
                .GetProperty(
                    destinationName)
                .GetProperty(
                    "Address")
                .GetString();

        Assert.Equal(
            expectedAddress,
            address);
    }
}
