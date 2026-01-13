using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.Entities;

/// <summary>
/// Role entity - represents a role with associated permissions.
/// </summary>
public class Role : Entity
{
    private readonly List<RolePermission> _permissions = new();

    private Role() { }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Role Create(string name, string? description = null, bool isSystemRole = false)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? description)
    {
        if (!IsSystemRole)
        {
            Description = description;
        }
    }

    public void AddPermission(string permission)
    {
        if (!_permissions.Any(p => p.Permission == permission))
        {
            _permissions.Add(RolePermission.Create(Id, permission));
        }
    }

    public void RemovePermission(string permission)
    {
        var existing = _permissions.FirstOrDefault(p => p.Permission == permission);
        if (existing != null)
        {
            _permissions.Remove(existing);
        }
    }
}

/// <summary>
/// User-Role association.
/// </summary>
public class UserRole : Entity
{
    private UserRole() { }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Role-Permission association.
/// </summary>
public class RolePermission : Entity
{
    private RolePermission() { }

    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = null!;

    public static RolePermission Create(Guid roleId, string permission)
    {
        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Permission = permission
        };
    }
}
