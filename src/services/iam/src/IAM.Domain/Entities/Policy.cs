using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.Entities;

/// <summary>
/// Policy entity for ABAC authorization.
/// </summary>
public class Policy : Entity
{
    private readonly List<PolicyCondition> _conditions = new();

    private Policy() { }

    public string Name { get; private set; } = null!;
    public string Resource { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public PolicyEffect Effect { get; private set; }
    public int Priority { get; private set; }
    public bool RequiresMfa { get; private set; }
    public bool RequiresOtpt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<PolicyCondition> Conditions => _conditions.AsReadOnly();

    public static Policy Create(
        string name,
        string resource,
        string action,
        PolicyEffect effect,
        int priority = 0,
        bool requiresMfa = false,
        bool requiresOtpt = false)
    {
        return new Policy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Resource = resource,
            Action = action,
            Effect = effect,
            Priority = priority,
            RequiresMfa = requiresMfa,
            RequiresOtpt = requiresOtpt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddCondition(string attribute, ConditionOperator @operator, string value)
    {
        _conditions.Add(PolicyCondition.Create(Id, attribute, @operator, value));
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Policy condition for attribute-based evaluation.
/// </summary>
public class PolicyCondition : Entity
{
    private PolicyCondition() { }

    public Guid PolicyId { get; private set; }
    public string Attribute { get; private set; } = null!;
    public ConditionOperator Operator { get; private set; }
    public string Value { get; private set; } = null!;

    public static PolicyCondition Create(Guid policyId, string attribute, ConditionOperator @operator, string value)
    {
        return new PolicyCondition
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Attribute = attribute,
            Operator = @operator,
            Value = value
        };
    }
}

public enum PolicyEffect
{
    Allow,
    Deny
}

public enum ConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    In,
    NotIn,
    Contains,
    StartsWith
}

/// <summary>
/// Policy-Role association for role-based conditions.
/// </summary>
public class PolicyRole : Entity
{
    private PolicyRole() { }

    public Guid PolicyId { get; private set; }
    public string RoleName { get; private set; } = null!;

    public static PolicyRole Create(Guid policyId, string roleName)
    {
        return new PolicyRole
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            RoleName = roleName
        };
    }
}
