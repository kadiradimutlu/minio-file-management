using System.Reflection;
using FileManagement.Api.Controllers;
using FileManagement.Identity.Api.Controllers;
using FileManagement.Identity.Infrastructure.Persistence;
using FileManagement.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace FileManagement.Identity.UnitTests.Security;

public sealed class AuthorizationBoundaryTests
{
    [Fact]
    public void AuthController_IsProtectedByDefault()
    {
        var authorize =
            typeof(AuthController)
                .GetCustomAttribute<
                    AuthorizeAttribute>();

        Assert.NotNull(authorize);
    }

    [Theory]
    [InlineData(
        nameof(AuthController.Register))]
    [InlineData(
        nameof(AuthController.Login))]
    public void PublicIdentityEndpoint_AllowsAnonymous(
        string methodName)
    {
        var method =
            typeof(AuthController)
                .GetMethod(methodName);

        Assert.NotNull(method);

        Assert.NotNull(
            method.GetCustomAttribute<
                AllowAnonymousAttribute>());
    }

    [Fact]
    public void AdminPing_RequiresAdminRole()
    {
        var method =
            typeof(AuthController)
                .GetMethod(
                    nameof(
                        AuthController
                            .AdminPing));

        Assert.NotNull(method);

        var authorize =
            method.GetCustomAttribute<
                AuthorizeAttribute>();

        Assert.NotNull(authorize);

        Assert.Equal(
            IdentityRoleNames.Admin,
            authorize.Roles);
    }

    [Fact]
    public void FilesController_RequiresAuthentication()
    {
        var authorize =
            typeof(FilesController)
                .GetCustomAttribute<
                    AuthorizeAttribute>();

        Assert.NotNull(authorize);
    }
}
