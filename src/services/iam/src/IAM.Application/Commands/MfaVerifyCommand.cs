using FluentValidation;
using IAM.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Jwt;
using Platform.Security.Mfa;
using Platform.Security.Session;

namespace IAM.Application.Commands;

/// <summary>
/// Command to verify MFA code and complete login.
/// </summary>
public record MfaVerifyCommand(
    string MfaToken,
    string Code,
    string FingerprintId,
    string IpAddress,
    string UserAgent) : IRequest<Result<LoginResponse>>;

public class MfaVerifyCommandValidator : AbstractValidator<MfaVerifyCommand>
{
    public MfaVerifyCommandValidator()
    {
        RuleFor(x => x.MfaToken)
            .NotEmpty().WithMessage("MFA token is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("MFA code is required")
            .Length(6).WithMessage("MFA code must be 6 digits")
            .Matches("^[0-9]+$").WithMessage("MFA code must contain only digits");

        RuleFor(x => x.FingerprintId)
            .NotEmpty().WithMessage("Fingerprint ID is required");
    }
}

public class MfaVerifyCommandHandler : IRequestHandler<MfaVerifyCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ISessionService _sessionService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<MfaVerifyCommandHandler> _logger;

    public MfaVerifyCommandHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        ISessionService sessionService,
        IJwtTokenService jwtTokenService,
        ILogger<MfaVerifyCommandHandler> logger)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _sessionService = sessionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(MfaVerifyCommand request, CancellationToken cancellationToken)
    {
        // Validate the MFA session
        var validationResult = await _sessionService.ValidateSessionAsync(
            new ValidateSessionRequest
            {
                SessionId = request.MfaToken,
                FingerprintId = request.FingerprintId,
                UserId = string.Empty // We'll get this from the session
            },
            cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid MFA session: {MfaToken}", request.MfaToken);
            return Result.Failure<LoginResponse>(
                Error.UnauthorizedError("Auth.InvalidMfaSession", "Invalid or expired MFA session"));
        }

        // For now, we need to get the user from the MFA token
        // In a real implementation, the session would contain the user ID
        // This is a simplified version
        
        // Revoke the temporary MFA session
        await _sessionService.RevokeSessionAsync(request.MfaToken, cancellationToken);

        // Since we don't have the user ID from the session in this simplified version,
        // we'll return an error. In production, the session would contain the user ID.
        return Result.Failure<LoginResponse>(
            Error.Failure("Auth.MfaNotImplemented", "MFA verification flow needs session user ID"));
    }
}
