using IAM.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Contracts.IAM;
using Platform.Security.Jwt;

namespace IAM.API.Controllers;

/// <summary>
/// OTPT (One-Time-Per-Transaction) controller for critical operations.
/// </summary>
[ApiController]
[Route("api/otpt")]
[Authorize]
public class OtptController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OtptController> _logger;

    public OtptController(IMediator mediator, ILogger<OtptController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Issues an OTPT token for a critical operation.
    /// </summary>
    [HttpPost("issue")]
    [ProducesResponseType(typeof(OtptResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IssueOtpt([FromBody] OtptRequestDto request)
    {
        var userId = User.GetUserId();
        var sessionId = User.GetSessionId();
        var fingerprintId = User.GetFingerprintId();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(fingerprintId))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://platform.gov.tr/errors/auth/invalid-session",
                Title = "Auth.InvalidSession",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid session or missing fingerprint"
            });
        }

        var command = new IssueOtptCommand(
            request.Route,
            sessionId,
            fingerprintId,
            userId,
            request.Nonce);

        var result = await _mediator.Send(command);

        return result.Match(
            token => Ok(token),
            error => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Type = "https://platform.gov.tr/errors/otpt/issue-failed",
                Title = error.Code,
                Status = StatusCodes.Status500InternalServerError,
                Detail = error.Message
            }));
    }
}
