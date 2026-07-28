using System.Security.Claims;
using FileManagement.Identity.Api.Models;
using FileManagement.Identity.Infrastructure.Persistence;
using FileManagement.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FileManagement.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController :
    ControllerBase
{
    private readonly UserManager<
        ApplicationUser> _userManager;
    private readonly SignInManager<
        ApplicationUser> _signInManager;
    private readonly IJwtTokenService
        _jwtTokenService;
    private readonly ILogger<
        AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>>
        Register(
            RegisterRequest request)
    {
        var normalizedEmail =
            request.Email.Trim();

        var existingUser =
            await _userManager
                .FindByEmailAsync(
                    normalizedEmail);

        if (existingUser is not null)
        {
            return Conflict(
                new ProblemDetails
                {
                    Status =
                        StatusCodes
                            .Status409Conflict,
                    Title =
                        "An account with this email address already exists."
                });
        }

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName =
                    normalizedEmail,
                Email =
                    normalizedEmail,
                EmailConfirmed = true,
                CreatedAtUtc =
                    DateTime.UtcNow
            };

        var createResult =
            await _userManager
                .CreateAsync(
                    user,
                    request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(
                CreateValidationProblem(
                    createResult));
        }

        var roleResult =
            await _userManager
                .AddToRoleAsync(
                    user,
                    IdentityRoleNames.User);

        if (!roleResult.Succeeded)
        {
            await _userManager
                .DeleteAsync(user);

            return BadRequest(
                CreateValidationProblem(
                    roleResult));
        }

        _logger.LogInformation(
            "Identity user {UserId} registered",
            user.Id);

        var response =
            await CreateAuthResponseAsync(
                user);

        return CreatedAtAction(
            nameof(GetCurrentUser),
            response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>>
        Login(
            LoginRequest request)
    {
        var user =
            await _userManager
                .FindByEmailAsync(
                    request.Email.Trim());

        if (user is null)
        {
            return InvalidCredentials();
        }

        var signInResult =
            await _signInManager
                .CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning(
                "Identity login failed for user {UserId}",
                user.Id);

            return InvalidCredentials();
        }

        _logger.LogInformation(
            "Identity user {UserId} logged in",
            user.Id);

        return Ok(
            await CreateAuthResponseAsync(
                user));
    }

    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<CurrentUserResponse>>
        GetCurrentUser()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (
            !Guid.TryParse(
                userIdValue,
                out var userId)
        )
        {
            return Unauthorized();
        }

        var user =
            await _userManager
                .FindByIdAsync(
                    userId.ToString());

        if (
            user is null ||
            string.IsNullOrWhiteSpace(
                user.Email)
        )
        {
            return Unauthorized();
        }

        var roles =
            await _userManager
                .GetRolesAsync(user);

        return Ok(
            new CurrentUserResponse(
                user.Id,
                user.Email,
                roles.ToArray()));
    }

    [HttpGet("admin/ping")]
    [Authorize(
        Roles =
            IdentityRoleNames.Admin)]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public IActionResult AdminPing()
    {
        return Ok(
            new
            {
                message =
                    "Admin access granted."
            });
    }

    private async Task<AuthResponse>
        CreateAuthResponseAsync(
            ApplicationUser user)
    {
        var roles =
            await _userManager
                .GetRolesAsync(user);

        var token =
            _jwtTokenService.CreateToken(
                user,
                roles.ToArray());

        return new AuthResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            user.Id,
            user.Email!,
            roles.ToArray());
    }

    private UnauthorizedObjectResult
        InvalidCredentials()
    {
        return Unauthorized(
            new ProblemDetails
            {
                Status =
                    StatusCodes
                        .Status401Unauthorized,
                Title =
                    "Invalid email address or password."
            });
    }

    private static ValidationProblemDetails
        CreateValidationProblem(
            IdentityResult result)
    {
        var errors =
            result.Errors
                .GroupBy(
                    error =>
                        error.Code)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.Select(
                                error =>
                                    error.Description)
                            .ToArray());

        return new ValidationProblemDetails(
            errors)
        {
            Status =
                StatusCodes
                    .Status400BadRequest,
            Title =
                "Identity validation failed."
        };
    }
}
