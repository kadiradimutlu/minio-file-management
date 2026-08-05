using FileManagement.Identity.Infrastructure.Options;
using FileManagement.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManagement.Identity.Infrastructure.Persistence;

public sealed class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<
        IdentityRole<Guid>> _roleManager;
    private readonly IdentityAdminOptions _adminOptions;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityAdminOptions> adminOptions,
        ILogger<IdentityDataSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        foreach (
            var roleName in
            IdentityRoleNames.All
        )
        {
            if (
                await _roleManager
                    .RoleExistsAsync(roleName)
            )
            {
                continue;
            }

            var roleResult =
                await _roleManager.CreateAsync(
                    new IdentityRole<Guid>(
                        roleName));

            EnsureSucceeded(
                roleResult,
                $"Role creation failed: {roleName}");
        }

        var adminUser =
            await _userManager.FindByEmailAsync(
                _adminOptions.Email);

        if (adminUser is null)
        {
            adminUser =
                new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName =
                        _adminOptions.Email,
                    Email =
                        _adminOptions.Email,
                    EmailConfirmed = true,
                    CreatedAtUtc =
                        DateTime.UtcNow
                };

            var createResult =
                await _userManager.CreateAsync(
                    adminUser,
                    _adminOptions.Password);

            EnsureSucceeded(
                createResult,
                "Initial admin account creation failed");

            _logger.LogInformation(
                "Initial Identity admin user {UserId} was created",
                adminUser.Id);
        }

        var requiredRoles = new[]
        {
            IdentityRoleNames.Admin,
            IdentityRoleNames.User
        };

        foreach (var roleName in requiredRoles)
        {
            if (
                await _userManager.IsInRoleAsync(
                    adminUser,
                    roleName)
            )
            {
                continue;
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    adminUser,
                    roleName);

            EnsureSucceeded(
                addRoleResult,
                $"Adding the admin user to role {roleName} failed");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(
                error =>
                    $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"{operation}. {errors}");
    }
}
