using IAM.Domain.Entities;
using IAM.Domain.ValueObjects;
using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.Repositories;

/// <summary>
/// Repository interface for User aggregate.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their roles.
    /// </summary>
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use.
    /// </summary>
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role.
    /// </summary>
    Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Role entity.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Update(Role role);
}

/// <summary>
/// Repository interface for Policy entity.
/// </summary>
public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Policy>> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Policy>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Policy policy, CancellationToken cancellationToken = default);
    void Update(Policy policy);
}
