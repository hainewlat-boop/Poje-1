using IAM.Domain.Entities;
using IAM.Domain.Repositories;
using IAM.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IAM.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IamDbContext _context;

    public UserRepository(IamDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

        if (role == null)
        {
            return Array.Empty<User>();
        }

        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == role.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User aggregate, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(aggregate, cancellationToken);
    }

    public void Update(User aggregate)
    {
        _context.Users.Update(aggregate);
    }
}

/// <summary>
/// Repository implementation for Role entity.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IamDbContext _context;

    public RoleRepository(IamDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.Permissions)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public void Update(Role role)
    {
        _context.Roles.Update(role);
    }
}

/// <summary>
/// Repository implementation for Policy entity.
/// </summary>
public class PolicyRepository : IPolicyRepository
{
    private readonly IamDbContext _context;

    public PolicyRepository(IamDbContext context)
    {
        _context = context;
    }

    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .Include(p => p.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .Include(p => p.Conditions)
            .Where(p => p.Resource == resource && p.Action == action && p.IsActive)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Policies
            .Include(p => p.Conditions)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        await _context.Policies.AddAsync(policy, cancellationToken);
    }

    public void Update(Policy policy)
    {
        _context.Policies.Update(policy);
    }
}
