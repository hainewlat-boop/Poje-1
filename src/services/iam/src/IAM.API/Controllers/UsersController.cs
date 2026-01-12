using IAM.Application.Commands;
using IAM.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Contracts.IAM;

namespace IAM.API.Controllers;

/// <summary>
/// User management controller.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.NationalId,
            request.PhoneNumber,
            request.Roles);

        var result = await _mediator.Send(command);

        return result.Match(
            user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, user),
            error => error.Type switch
            {
                Platform.BuildingBlocks.Result.ErrorType.Conflict => Conflict(CreateProblem(error)),
                Platform.BuildingBlocks.Result.ErrorType.Validation => BadRequest(CreateProblem(error)),
                _ => BadRequest(CreateProblem(error))
            });
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);

        return result.Match(
            user => Ok(user),
            error => NotFound(CreateProblem(error)));
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);

        return result.Match(
            user => Ok(user),
            error => NotFound(CreateProblem(error)));
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
                Platform.BuildingBlocks.Result.ErrorType.Conflict => StatusCodes.Status409Conflict,
                Platform.BuildingBlocks.Result.ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            },
            Detail = error.Message
        };
    }
}
