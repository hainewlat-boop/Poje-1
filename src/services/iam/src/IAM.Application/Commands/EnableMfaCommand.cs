using FluentValidation;
using IAM.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Domain;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Cryptography;
using Platform.Security.Mfa;

namespace IAM.Application.Commands;

/// <summary>
/// Command to start MFA enrollment.
/// </summary>
public record EnableMfaCommand(
    Guid UserId,
    string Password) : IRequest<Result<MfaEnrollResponse>>;

public class EnableMfaCommandValidator : AbstractValidator<EnableMfaCommand>
{
    public EnableMfaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required for MFA enrollment");
    }
}

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, Result<MfaEnrollResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;
    private readonly ILogger<EnableMfaCommandHandler> _logger;

    public EnableMfaCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITotpService totpService,
        ILogger<EnableMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollResponse>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Result.Failure<MfaEnrollResponse>(
                Error.NotFoundError("User.NotFound", "User not found"));
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password during MFA enrollment for user: {UserId}", request.UserId);
            return Result.Failure<MfaEnrollResponse>(
                Error.UnauthorizedError("Auth.InvalidPassword", "Invalid password"));
        }

        if (user.MfaEnabled)
        {
            return Result.Failure<MfaEnrollResponse>(
                Error.ConflictError("Mfa.AlreadyEnabled", "MFA is already enabled for this user"));
        }

        // Generate TOTP secret
        var totpSecret = _totpService.GenerateSecret("Platform", user.Email.Value);

        // Store secret temporarily (in production, use secure temporary storage)
        // The secret will be confirmed when user verifies the first code
        
        _logger.LogInformation("MFA enrollment initiated for user: {UserId}", request.UserId);

        return Result.Success(new MfaEnrollResponse
        {
            Secret = totpSecret.Secret,
            QrCodeUri = totpSecret.ProvisioningUri,
            ManualEntryKey = totpSecret.ManualEntryKey
        });
    }
}

/// <summary>
/// Command to confirm MFA enrollment with a verification code.
/// </summary>
public record ConfirmMfaCommand(
    Guid UserId,
    string Secret,
    string Code) : IRequest<Result<bool>>;

public class ConfirmMfaCommandValidator : AbstractValidator<ConfirmMfaCommand>
{
    public ConfirmMfaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Secret is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Length(6).WithMessage("Code must be 6 digits");
    }
}

public class ConfirmMfaCommandHandler : IRequestHandler<ConfirmMfaCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmMfaCommandHandler> _logger;

    public ConfirmMfaCommandHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ConfirmMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Result.Failure<bool>(
                Error.NotFoundError("User.NotFound", "User not found"));
        }

        // Verify the TOTP code
        if (!_totpService.VerifyCode(request.Secret, request.Code))
        {
            _logger.LogWarning("Invalid MFA code during enrollment for user: {UserId}", request.UserId);
            return Result.Failure<bool>(
                Error.ValidationError("Mfa.InvalidCode", "Invalid verification code"));
        }

        // Enable MFA
        user.EnableMfa(request.Secret);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MFA enabled for user: {UserId}", request.UserId);

        return Result.Success(true);
    }
}
