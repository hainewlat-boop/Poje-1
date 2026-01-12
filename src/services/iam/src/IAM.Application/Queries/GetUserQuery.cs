using IAM.Domain.Repositories;
using MediatR;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;

namespace IAM.Application.Queries;

/// <summary>
/// Query to get user by ID.
/// </summary>
public record GetUserQuery(Guid UserId) : IRequest<Result<UserResponse>>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public GetUserQueryHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Result.Failure<UserResponse>(
                Error.NotFoundError("User.NotFound", "User not found"));
        }

        // Get role names
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        return Result.Success(new UserResponse
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.Name.FirstName,
            LastName = user.Name.LastName,
            PhoneNumber = user.PhoneNumber?.Value,
            MfaEnabled = user.MfaEnabled,
            IsActive = user.Status == Domain.Enums.UserStatus.Active,
            CreatedAt = user.CreatedAt,
            Roles = roleNames
        });
    }
}
