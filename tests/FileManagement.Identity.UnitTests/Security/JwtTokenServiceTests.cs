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
}
