using IAM.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Contracts.IAM;

namespace IAM.API.Controllers;

/// <summary>
/// MFA management controller.
/// </summary>
[ApiController]
[Route("api/mfa")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MfaController> _logger;

    public MfaController(IMediator mediator, ILogger<MfaController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Starts MFA enrollment for the current user.
    /// </summary>
    [HttpPost("enroll")]
    [ProducesResponseType(typeof(MfaEnrollResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnrollMfa([FromBody] MfaEnrollRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new EnableMfaCommand(userId, request.Password);
        var result = await _mediator.Send(command);

        return result.Match(
            response => Ok(response),
            error => error.Type switch
            {
                Platform.BuildingBlocks.Result.ErrorType.Conflict => Conflict(CreateProblem(error)),
                Platform.BuildingBlocks.Result.ErrorType.Unauthorized => Unauthorized(CreateProblem(error)),
                _ => BadRequest(CreateProblem(error))
            });
    }

    /// <summary>
    /// Confirms MFA enrollment with a verification code.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmMfa([FromBody] MfaConfirmRequest request, [FromQuery] string secret)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new ConfirmMfaCommand(userId, secret, request.Code);
        var result = await _mediator.Send(command);

        return result.Match(
            _ => Ok(new { message = "MFA enabled successfully" }),
            error => BadRequest(CreateProblem(error)));
    }

    private static ProblemDetails CreateProblem(Platform.BuildingBlocks.Result.Error error)
    {
        return new ProblemDetails
        {
            Type = $"https://platform.gov.tr/errors/{error.Code.ToLowerInvariant().Replace(".", "/")}",
            Title = error.Code,
            Status = error.Type switch
            {
                Platform.BuildingBlocks.Result.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                Platform.BuildingBlocks.Result.ErrorType.Conflict => StatusCodes.Status409Conflict,
                Platform.BuildingBlocks.Result.ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            },
            Detail = error.Message
        };
    }
}
