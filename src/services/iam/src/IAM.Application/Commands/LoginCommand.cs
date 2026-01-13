using FluentValidation;
using IAM.Domain.Repositories;
using IAM.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Cryptography;
using Platform.Security.Jwt;
using Platform.Security.RateLimiting;
using Platform.Security.Session;

namespace IAM.Application.Commands;

/// <summary>
/// Login command - authenticates user and creates session.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string FingerprintId,
    string IpAddress,
    string UserAgent,
    string? DeviceId) : IRequest<Result<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.FingerprintId)
            .NotEmpty().WithMessage("Fingerprint ID is required");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionService _sessionService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionService sessionService,
        IJwtTokenService jwtTokenService,
        IRateLimiter rateLimiter,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
        _jwtTokenService = jwtTokenService;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Rate limiting check
        var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(
            new RateLimitRequest
            {
                Key = $"login:{request.Email}",
                Policy = new RateLimitPolicy { Limit = 5, Window = TimeSpan.FromMinutes(1) }
            },
            cancellationToken);

        if (!rateLimitResult.IsAllowed)
        {
            _logger.LogWarning("Login rate limit exceeded for {Email}", request.Email);
            return Result.Failure<LoginResponse>(
                Error.LockedError("Auth.RateLimited", $"Too many login attempts. Try again in {rateLimitResult.RetryAfterSeconds} seconds"));
        }

        // Get user by email
        if (!Email.TryCreate(request.Email, out var email))
        {
            return Result.Failure<LoginResponse>(
                Error.ValidationError("Auth.InvalidEmail", "Invalid email format"));
        }

        var user = await _userRepository.GetByEmailAsync(email!, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            return Result.Failure<LoginResponse>(
                Error.UnauthorizedError("Auth.InvalidCredentials", "Invalid email or password"));
        }

        // Check if user is locked
        if (user.IsLocked())
        {
            _logger.LogWarning("Login attempt for locked user: {UserId}", user.Id);
            return Result.Failure<LoginResponse>(
                Error.LockedError("Auth.AccountLocked", "Account is temporarily locked"));
        }

        // Check if user is active
        if (user.Status != Domain.Enums.UserStatus.Active)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            return Result.Failure<LoginResponse>(
                Error.ForbiddenError("Auth.AccountInactive", "Account is not active"));
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _rateLimiter.RecordFailedLoginAsync(request.Email, cancellationToken);

            _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
            return Result.Failure<LoginResponse>(
                Error.UnauthorizedError("Auth.InvalidCredentials", "Invalid email or password"));
        }

        // Clear failed attempts on successful authentication
        user.RecordSuccessfulLogin();
        await _rateLimiter.ClearFailedLoginsAsync(request.Email, cancellationToken);

        // Check if MFA is required
        if (user.MfaEnabled)
        {
            _logger.LogInformation("MFA required for user: {UserId}", user.Id);
            
            // Create temporary MFA token
            var mfaSession = await _sessionService.CreateSessionAsync(
                new CreateSessionRequest
                {
                    UserId = user.Id.ToString(),
                    FingerprintId = request.FingerprintId,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent,
                    DeviceId = request.DeviceId
                },
                cancellationToken);

            return Result.Success(new LoginResponse
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                AccessTokenExpiresAt = DateTime.UtcNow,
                RefreshTokenExpiresAt = DateTime.UtcNow,
                RequiresMfa = true,
                MfaToken = mfaSession.SessionId
            });
        }

        // Create session and tokens
        return await CreateSessionAndTokens(user, request, cancellationToken);
    }

    private async Task<Result<LoginResponse>> CreateSessionAndTokens(
        Domain.Entities.User user,
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var userWithRoles = await _userRepository.GetByIdWithRolesAsync(user.Id, cancellationToken);
        var roles = userWithRoles?.UserRoles.Select(ur => ur.RoleId.ToString()).ToList() ?? new List<string>();

        var session = await _sessionService.CreateSessionAsync(
            new CreateSessionRequest
            {
                UserId = user.Id.ToString(),
                FingerprintId = request.FingerprintId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                DeviceId = request.DeviceId
            },
            cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(new TokenClaims
        {
            UserId = user.Id.ToString(),
            SessionId = session.SessionId,
            Email = user.Email.Value,
            Roles = roles,
            FingerprintId = request.FingerprintId,
            MfaVerified = false
        });

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return Result.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = session.RefreshToken,
            AccessTokenExpiresAt = session.ExpiresAt,
            RefreshTokenExpiresAt = session.RefreshExpiresAt,
            RequiresMfa = false,
            MfaToken = null
        });
    }
}
