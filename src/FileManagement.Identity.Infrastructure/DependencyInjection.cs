using System.Security.Claims;
using System.Text;
using FileManagement.Identity.Infrastructure.Options;
using FileManagement.Identity.Infrastructure.Persistence;
using FileManagement.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FileManagement.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection
        AddIdentityInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "IdentityPostgreSql");

        if (
            string.IsNullOrWhiteSpace(
                connectionString)
        )
        {
            throw new InvalidOperationException(
                "ConnectionStrings:IdentityPostgreSql is not configured.");
        }

        var jwtOptions =
            ReadJwtOptions(configuration);

        services.AddDbContext<
            IdentityDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString));

        services
            .AddIdentityCore<
                ApplicationUser>(
                options =>
                {
                    options.User
                        .RequireUniqueEmail = true;

                    options.Password
                        .RequiredLength = 8;

                    options.Password
                        .RequireDigit = true;

                    options.Password
                        .RequireLowercase = true;

                    options.Password
                        .RequireUppercase = true;

                    options.Password
                        .RequireNonAlphanumeric = false;

                    options.Lockout
                        .MaxFailedAccessAttempts = 5;

                    options.Lockout
                        .DefaultLockoutTimeSpan =
                            TimeSpan.FromMinutes(5);
                })
            .AddRoles<
                IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<
                IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(
                configuration.GetSection(
                    JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Audience),
                "Jwt:Audience is required.")
            .Validate(
                options =>
                    options.SigningKey.Length >= 32,
                "Jwt:SigningKey must contain at least 32 characters.")
            .Validate(
                options =>
                    options.ExpirationMinutes
                        is >= 5 and <= 1440,
                "Jwt:ExpirationMinutes must be between 5 and 1440.")
            .ValidateOnStart();

        services
            .AddOptions<
                IdentityAdminOptions>()
            .Bind(
                configuration.GetSection(
                    IdentityAdminOptions
                        .SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Email),
                "IdentityAdmin:Email is required.")
            .Validate(
                options =>
                    options.Password.Length >= 12,
                "IdentityAdmin:Password must contain at least 12 characters.")
            .ValidateOnStart();

        services
            .AddAuthentication(
                JwtBearerDefaults
                    .AuthenticationScheme)
            .AddJwtBearer(
                options =>
                {
                    options.MapInboundClaims =
                        false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer =
                                jwtOptions.Issuer,
                            ValidateAudience = true,
                            ValidAudience =
                                jwtOptions.Audience,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey =
                                true,
                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8
                                        .GetBytes(
                                            jwtOptions
                                                .SigningKey)),
                            ClockSkew =
                                TimeSpan.FromSeconds(
                                    30),
                            NameClaimType =
                                ClaimTypes.Name,
                            RoleClaimType =
                                ClaimTypes.Role
                        };
                });

        services.AddAuthorization();

        services.AddSingleton<
            TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            IJwtTokenService,
            JwtTokenService>();

        services.AddScoped<
            IdentityDataSeeder>();

        return services;
    }

    private static JwtOptions ReadJwtOptions(
        IConfiguration configuration)
    {
        var options =
            new JwtOptions
            {
                Issuer =
                    configuration[
                        "Jwt:Issuer"] ??
                    string.Empty,
                Audience =
                    configuration[
                        "Jwt:Audience"] ??
                    string.Empty,
                SigningKey =
                    configuration[
                        "Jwt:SigningKey"] ??
                    string.Empty
            };

        var expirationValue =
            configuration[
                "Jwt:ExpirationMinutes"];

        if (
            int.TryParse(
                expirationValue,
                out var expirationMinutes)
        )
        {
            options.ExpirationMinutes =
                expirationMinutes;
        }

        if (
            string.IsNullOrWhiteSpace(
                options.Issuer) ||
            string.IsNullOrWhiteSpace(
                options.Audience) ||
            options.SigningKey.Length < 32 ||
            options.ExpirationMinutes
                is < 5 or > 1440
        )
        {
            throw new InvalidOperationException(
                "JWT configuration is invalid.");
        }

        return options;
    }
}
