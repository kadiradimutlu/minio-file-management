using FileManagement.Identity.Infrastructure.Persistence;

namespace FileManagement.Identity.Infrastructure.Security;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles);
}
