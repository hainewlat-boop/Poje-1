using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Otpt;

namespace IAM.Application.Commands;

/// <summary>
/// Command to issue an OTPT token for a critical operation.
/// </summary>
public record IssueOtptCommand(
    string Route,
    string SessionId,
    string FingerprintId,
    string UserId,
    string? Nonce) : IRequest<Result<OtptResponseDto>>;

public class IssueOtptCommandValidator : AbstractValidator<IssueOtptCommand>
{
    public IssueOtptCommandValidator()
    {
        RuleFor(x => x.Route)
            .NotEmpty().WithMessage("Route is required");

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required");

        RuleFor(x => x.FingerprintId)
            .NotEmpty().WithMessage("Fingerprint ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class IssueOtptCommandHandler : IRequestHandler<IssueOtptCommand, Result<OtptResponseDto>>
{
    private readonly IOtptService _otptService;
    private readonly ILogger<IssueOtptCommandHandler> _logger;

    public IssueOtptCommandHandler(
        IOtptService otptService,
        ILogger<IssueOtptCommandHandler> logger)
    {
        _otptService = otptService;
        _logger = logger;
    }

    public async Task<Result<OtptResponseDto>> Handle(IssueOtptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _otptService.IssueTokenAsync(
                new OtptRequest
                {
                    Route = request.Route,
                    SessionId = request.SessionId,
                    FingerprintId = request.FingerprintId,
                    UserId = request.UserId,
                    Nonce = request.Nonce,
                    RequiresMfa = false // Would be determined by policy
                },
                cancellationToken);

            _logger.LogInformation(
                "OTPT issued for user {UserId} route {Route}",
                request.UserId,
                request.Route);

            return Result.Success(new OtptResponseDto
            {
                Token = token.Token,
                Nonce = token.Nonce,
                ExpiresAt = token.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to issue OTPT for user {UserId}", request.UserId);
            return Result.Failure<OtptResponseDto>(
                Error.Failure("Otpt.IssueFailed", "Failed to issue OTPT token"));
        }
    }
}

/// <summary>
/// Command to consume (validate and invalidate) an OTPT token.
/// </summary>
public record ConsumeOtptCommand(
    string Token,
    string Route,
    string SessionId,
    string FingerprintId,
    string UserId,
    string Nonce) : IRequest<Result<bool>>;

public class ConsumeOtptCommandValidator : AbstractValidator<ConsumeOtptCommand>
{
    public ConsumeOtptCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("OTPT token is required");

        RuleFor(x => x.Route)
            .NotEmpty().WithMessage("Route is required");

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required");

        RuleFor(x => x.FingerprintId)
            .NotEmpty().WithMessage("Fingerprint ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Nonce)
            .NotEmpty().WithMessage("Nonce is required");
    }
}

public class ConsumeOtptCommandHandler : IRequestHandler<ConsumeOtptCommand, Result<bool>>
{
    private readonly IOtptService _otptService;
    private readonly ILogger<ConsumeOtptCommandHandler> _logger;

    public ConsumeOtptCommandHandler(
        IOtptService otptService,
        ILogger<ConsumeOtptCommandHandler> logger)
    {
        _otptService = otptService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ConsumeOtptCommand request, CancellationToken cancellationToken)
    {
        var result = await _otptService.ConsumeTokenAsync(
            new OtptConsumption
            {
                Token = request.Token,
                Route = request.Route,
                SessionId = request.SessionId,
                FingerprintId = request.FingerprintId,
                UserId = request.UserId,
                Nonce = request.Nonce
            },
            cancellationToken);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "OTPT validation failed for user {UserId} route {Route}: {Error}",
                request.UserId,
                request.Route,
                result.Error);

            return Result.Failure<bool>(
                Error.UnauthorizedError("Otpt.Invalid", result.Error ?? "Invalid OTPT token"));
        }

        _logger.LogInformation(
            "OTPT consumed successfully for user {UserId} route {Route}",
            request.UserId,
            request.Route);

        return Result.Success(true);
    }
}
