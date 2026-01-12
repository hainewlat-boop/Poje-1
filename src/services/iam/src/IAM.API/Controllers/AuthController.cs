using IAM.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Contracts.IAM;

namespace IAM.API.Controllers;

/// <summary>
/// Authentication controller for login, MFA, and token management.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.FingerprintId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            request.DeviceId);

        var result = await _mediator.Send(command);

        return result.Match(
            response => Ok(response),
            error => error.Type switch
            {
                Platform.BuildingBlocks.Result.ErrorType.Unauthorized => Unauthorized(CreateProblem(error)),
                Platform.BuildingBlocks.Result.ErrorType.Locked => Problem(
                    statusCode: StatusCodes.Status423Locked,
                    title: error.Code,
                    detail: error.Message),
                Platform.BuildingBlocks.Result.ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateProblem(error)),
                _ => BadRequest(CreateProblem(error))
            });
    }

    /// <summary>
    /// Verifies MFA code and completes login.
    /// </summary>
    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request)
    {
        var command = new MfaVerifyCommand(
            request.MfaToken,
            request.Code,
            request.FingerprintId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send(command);

        return result.Match(
            response => Ok(response),
            error => Unauthorized(CreateProblem(error)));
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken,
            request.FingerprintId);

        var result = await _mediator.Send(command);

        return result.Match(
            response => Ok(response),
            error => Unauthorized(CreateProblem(error)));
    }

    /// <summary>
    /// Logs out the current session or all sessions.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        // TODO: Implement logout command
        _logger.LogInformation("User logged out");
        return NoContent();
    }

    private static ProblemDetails CreateProblem(Platform.BuildingBlocks.Result.Error error)
    {
        return new ProblemDetails
        {
            Type = $"https://platform.gov.tr/errors/{error.Code.ToLowerInvariant().Replace(".", "/")}",
            Title = error.Code,
            Status = error.Type switch
            {
                Platform.BuildingBlocks.Result.ErrorType.NotFound => StatusCodes.Status404NotFound,
                Platform.BuildingBlocks.Result.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                Platform.BuildingBlocks.Result.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                Platform.BuildingBlocks.Result.ErrorType.Conflict => StatusCodes.Status409Conflict,
                Platform.BuildingBlocks.Result.ErrorType.Locked => StatusCodes.Status423Locked,
                Platform.BuildingBlocks.Result.ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            },
            Detail = error.Message
        };
    }
}
