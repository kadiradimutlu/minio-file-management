using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileManagement.Identity.Infrastructure.Options;
using FileManagement.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FileManagement.Identity.Infrastructure.Security;

public sealed class JwtTokenService :
    IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public JwtTokenResult CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles)
    {
        if (
            string.IsNullOrWhiteSpace(
                user.Email)
        )
        {
            throw new InvalidOperationException(
                "A JWT cannot be created for a user without an email address.");
        }

        var issuedAt =
            _timeProvider.GetUtcNow();

        var expiresAt =
            issuedAt.AddMinutes(
                _options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")),
            new(
                JwtRegisteredClaimNames.Email,
                user.Email),
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),
            new(
                ClaimTypes.Name,
                user.Email)
        };

        claims.AddRange(
            roles.Select(
                role =>
                    new Claim(
                        ClaimTypes.Role,
                        role)));

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _options.SigningKey));

        var signingCredentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore:
                    issuedAt.UtcDateTime,
                expires:
                    expiresAt.UtcDateTime,
                signingCredentials:
                    signingCredentials);

        var serializedToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new JwtTokenResult(
            serializedToken,
            expiresAt);
    }
}
