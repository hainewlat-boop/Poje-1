using FluentValidation;
using IAM.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Jwt;
using Platform.Security.Session;

namespace IAM.Application.Commands;

/// <summary>
/// Command to refresh access token using refresh token.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    string FingerprintId) : IRequest<Result<LoginResponse>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");

        RuleFor(x => x.FingerprintId)
            .NotEmpty().WithMessage("Fingerprint ID is required");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionService _sessionService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ISessionService sessionService,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _sessionService = sessionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Refresh the session
        var refreshResult = await _sessionService.RefreshSessionAsync(
            new RefreshSessionRequest
            {
                RefreshToken = request.RefreshToken,
                FingerprintId = request.FingerprintId
            },
            cancellationToken);

        if (!refreshResult.IsSuccess || refreshResult.NewSession == null)
        {
            _logger.LogWarning("Token refresh failed: {Error}", refreshResult.Error);
            
            return Result.Failure<LoginResponse>(
                Error.UnauthorizedError("Auth.RefreshFailed", refreshResult.Error ?? "Token refresh failed"));
        }

        // We need to get user info from the session
        // In a real implementation, we'd extract user ID from the refresh token
        // For now, return a placeholder response
        
        _logger.LogInformation("Token refreshed successfully");

        return Result.Success(new LoginResponse
        {
            AccessToken = "new-access-token", // Would be generated with proper user info
            RefreshToken = refreshResult.NewSession.RefreshToken,
            AccessTokenExpiresAt = refreshResult.NewSession.ExpiresAt,
            RefreshTokenExpiresAt = refreshResult.NewSession.RefreshExpiresAt,
            RequiresMfa = false,
            MfaToken = null
        });
    }
}
