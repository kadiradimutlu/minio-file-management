using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileManagement.Identity.Infrastructure.Options;
using FileManagement.Identity.Infrastructure.Persistence;
using FileManagement.Identity.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FileManagement.Identity.UnitTests.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_CreatesValidSignedToken()
    {
        var options =
            new JwtOptions
            {
                Issuer =
                    "identity-tests",
                Audience =
                    "file-services-tests",
                SigningKey =
                    "0123456789abcdef0123456789abcdef",
                ExpirationMinutes = 60
            };

        var service =
            new JwtTokenService(
                Options.Create(options),
                TimeProvider.System);

        var userId =
            Guid.NewGuid();

        var user =
            new ApplicationUser
            {
                Id = userId,
                UserName =
                    "user@example.com",
                Email =
                    "user@example.com"
            };

        var result =
            service.CreateToken(
                user,
                [
                    IdentityRoleNames.User,
                    IdentityRoleNames.Admin
                ]);

        var handler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

        var principal =
            handler.ValidateToken(
                result.AccessToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer =
                        options.Issuer,
                    ValidateAudience = true,
                    ValidAudience =
                        options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey =
                        true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8
                                .GetBytes(
                                    options.SigningKey)),
                    ClockSkew =
                        TimeSpan.Zero,
                    NameClaimType =
                        ClaimTypes.Name,
                    RoleClaimType =
                        ClaimTypes.Role
                },
                out var validatedToken);

        Assert.IsType<
            JwtSecurityToken>(
            validatedToken);

        Assert.Equal(
            userId.ToString(),
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier));

        Assert.Equal(
            "user@example.com",
            principal.FindFirstValue(
                ClaimTypes.Name));

        Assert.Contains(
            principal.Claims,
            claim =>
                claim.Type ==
                    ClaimTypes.Role &&
                claim.Value ==
                    IdentityRoleNames.User);

        Assert.Contains(
            principal.Claims,
            claim =>
                claim.Type ==
                    ClaimTypes.Role &&
                claim.Value ==
                    IdentityRoleNames.Admin);

        Assert.True(
            result.ExpiresAtUtc >
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ValidateToken_WithWrongIssuer_RejectsToken()
    {
        var options =
            CreateOptions();

        var token =
            CreateToken(options);

        var validationOptions =
            CreateOptions();

        validationOptions.Issuer =
            "unexpected-issuer";

        Assert.Throws<
            SecurityTokenInvalidIssuerException>(
            () =>
                ValidateToken(
                    token,
                    validationOptions));
    }

    [Fact]
    public void ValidateToken_WithWrongAudience_RejectsToken()
    {
        var options =
            CreateOptions();

        var token =
            CreateToken(options);

        var validationOptions =
            CreateOptions();

        validationOptions.Audience =
            "unexpected-audience";

        Assert.Throws<
            SecurityTokenInvalidAudienceException>(
            () =>
                ValidateToken(
                    token,
                    validationOptions));
    }

    [Fact]
    public void ValidateToken_WithWrongSigningKey_RejectsToken()
    {
        var options =
            CreateOptions();

        var token =
            CreateToken(options);

        var validationOptions =
            CreateOptions();

        validationOptions.SigningKey =
            "abcdef0123456789abcdef0123456789";

        Assert.ThrowsAny<
            SecurityTokenException>(
            () =>
                ValidateToken(
                    token,
                    validationOptions));
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_RejectsToken()
    {
        var options =
            CreateOptions();

        options.ExpirationMinutes = 5;

        var issuedAtUtc =
            new DateTimeOffset(
                2020,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

        var service =
            new JwtTokenService(
                Options.Create(options),
                new FixedTimeProvider(
                    issuedAtUtc));

        var token =
            service.CreateToken(
                    CreateUser(),
                    [IdentityRoleNames.User])
                .AccessToken;

        Assert.Throws<
            SecurityTokenExpiredException>(
            () =>
                ValidateToken(
                    token,
                    options));
    }

    [Fact]
    public void CreateToken_WithMissingEmail_Throws()
    {
        var service =
            new JwtTokenService(
                Options.Create(
                    CreateOptions()),
                TimeProvider.System);

        var user =
            CreateUser();

        user.Email = null;

        Assert.Throws<
            InvalidOperationException>(
            () =>
                service.CreateToken(
                    user,
                    [IdentityRoleNames.User]));
    }

    private static JwtOptions CreateOptions()
    {
        return new JwtOptions
        {
            Issuer =
                "identity-tests",
            Audience =
                "file-services-tests",
            SigningKey =
                "0123456789abcdef0123456789abcdef",
            ExpirationMinutes = 60
        };
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName =
                "user@example.com",
            Email =
                "user@example.com"
        };
    }

    private static string CreateToken(
        JwtOptions options)
    {
        var service =
            new JwtTokenService(
                Options.Create(options),
                TimeProvider.System);

        return service.CreateToken(
                CreateUser(),
                [IdentityRoleNames.User])
            .AccessToken;
    }

    private static ClaimsPrincipal
        ValidateToken(
            string token,
            JwtOptions options)
    {
        var handler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

        return handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer =
                    options.Issuer,
                ValidateAudience = true,
                ValidAudience =
                    options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey =
                    true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            options.SigningKey)),
                ClockSkew =
                    TimeSpan.Zero,
                NameClaimType =
                    ClaimTypes.Name,
                RoleClaimType =
                    ClaimTypes.Role
            },
            out _);
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset
            GetUtcNow()
        {
            return utcNow;
        }
    }
}
