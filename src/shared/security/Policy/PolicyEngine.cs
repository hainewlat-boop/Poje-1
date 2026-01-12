using Microsoft.Extensions.Logging;

namespace Platform.Security.Policy;

/// <summary>
/// Policy engine for RBAC + ABAC authorization.
/// Evaluates policies based on roles and attributes.
/// </summary>
public interface IPolicyEngine
{
    /// <summary>
    /// Evaluates whether an action is permitted based on the policy context.
    /// </summary>
    Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for policy evaluation.
/// </summary>
public record PolicyContext
{
    public required string UserId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required IDictionary<string, object> Attributes { get; init; }
}

/// <summary>
/// Result of policy evaluation.
/// </summary>
public record PolicyEvaluationResult
{
    public bool IsAllowed { get; init; }
    public string? DenialReason { get; init; }
    public bool RequiresMfa { get; init; }
    public bool RequiresOtpt { get; init; }
    public IReadOnlyList<string> AppliedPolicies { get; init; } = Array.Empty<string>();

    public static PolicyEvaluationResult Allow(IReadOnlyList<string>? policies = null, bool requiresMfa = false, bool requiresOtpt = false) => 
        new() 
        { 
            IsAllowed = true, 
            AppliedPolicies = policies ?? Array.Empty<string>(),
            RequiresMfa = requiresMfa,
            RequiresOtpt = requiresOtpt
        };

    public static PolicyEvaluationResult Deny(string reason) => 
        new() { IsAllowed = false, DenialReason = reason };
}

/// <summary>
/// Policy definition.
/// </summary>
public record PolicyDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public required PolicyEffect Effect { get; init; }
    public IReadOnlyList<string> RequiredRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PolicyCondition> Conditions { get; init; } = Array.Empty<PolicyCondition>();
    public bool RequiresMfa { get; init; }
    public bool RequiresOtpt { get; init; }
    public int Priority { get; init; }
}

public enum PolicyEffect
{
    Allow,
    Deny
}

/// <summary>
/// Condition for policy evaluation.
/// </summary>
public record PolicyCondition
{
    public required string Attribute { get; init; }
    public required PolicyOperator Operator { get; init; }
    public required object Value { get; init; }
}

public enum PolicyOperator
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
    StartsWith,
    Between
}

/// <summary>
/// Repository for policy definitions.
/// </summary>
public interface IPolicyRepository
{
    Task<IReadOnlyList<PolicyDefinition>> GetPoliciesForResourceAsync(string resource, string action, CancellationToken cancellationToken = default);
    Task<PolicyDefinition?> GetPolicyByIdAsync(string policyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of policy engine.
/// </summary>
public class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(
        IPolicyRepository policyRepository,
        ILogger<PolicyEngine> logger)
    {
        _policyRepository = policyRepository;
        _logger = logger;
    }

    public async Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        var policies = await _policyRepository.GetPoliciesForResourceAsync(
            context.Resource, 
            context.Action, 
            cancellationToken);

        if (!policies.Any())
        {
            _logger.LogWarning(
                "No policies found for resource {Resource} action {Action}",
                context.Resource,
                context.Action);
            
            // Default deny if no policies defined
            return PolicyEvaluationResult.Deny("No policies defined for this resource");
        }

        // Sort by priority (higher priority first)
        var sortedPolicies = policies.OrderByDescending(p => p.Priority).ToList();
        var appliedPolicies = new List<string>();
        var requiresMfa = false;
        var requiresOtpt = false;

        foreach (var policy in sortedPolicies)
        {
            var matches = EvaluatePolicy(policy, context);

            if (matches)
            {
                appliedPolicies.Add(policy.Id);
                requiresMfa |= policy.RequiresMfa;
                requiresOtpt |= policy.RequiresOtpt;

                if (policy.Effect == PolicyEffect.Deny)
                {
                    _logger.LogInformation(
                        "Access denied by policy {PolicyId} for user {UserId} on {Resource}/{Action}",
                        policy.Id,
                        context.UserId,
                        context.Resource,
                        context.Action);

                    return PolicyEvaluationResult.Deny($"Denied by policy: {policy.Name}");
                }
            }
        }

        // Check if at least one allow policy matched
        if (appliedPolicies.Any())
        {
            _logger.LogInformation(
                "Access allowed for user {UserId} on {Resource}/{Action}. Policies: {Policies}",
                context.UserId,
                context.Resource,
                context.Action,
                string.Join(", ", appliedPolicies));

            return PolicyEvaluationResult.Allow(appliedPolicies, requiresMfa, requiresOtpt);
        }

        return PolicyEvaluationResult.Deny("No matching policy found");
    }

    private bool EvaluatePolicy(PolicyDefinition policy, PolicyContext context)
    {
        // Check required roles
        if (policy.RequiredRoles.Any())
        {
            var hasRequiredRole = policy.RequiredRoles.Any(r => context.Roles.Contains(r));
            if (!hasRequiredRole)
            {
                return false;
            }
        }

        // Check conditions
        foreach (var condition in policy.Conditions)
        {
            if (!context.Attributes.TryGetValue(condition.Attribute, out var attributeValue))
            {
                return false;
            }

            if (!EvaluateCondition(condition, attributeValue))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateCondition(PolicyCondition condition, object attributeValue)
    {
        return condition.Operator switch
        {
            PolicyOperator.Equals => Equals(attributeValue, condition.Value),
            PolicyOperator.NotEquals => !Equals(attributeValue, condition.Value),
            PolicyOperator.GreaterThan => Compare(attributeValue, condition.Value) > 0,
            PolicyOperator.LessThan => Compare(attributeValue, condition.Value) < 0,
            PolicyOperator.GreaterThanOrEqual => Compare(attributeValue, condition.Value) >= 0,
            PolicyOperator.LessThanOrEqual => Compare(attributeValue, condition.Value) <= 0,
            PolicyOperator.In => IsIn(attributeValue, condition.Value),
            PolicyOperator.NotIn => !IsIn(attributeValue, condition.Value),
            PolicyOperator.Contains => Contains(attributeValue, condition.Value),
            PolicyOperator.StartsWith => StartsWith(attributeValue, condition.Value),
            PolicyOperator.Between => IsBetween(attributeValue, condition.Value),
            _ => false
        };
    }

    private static int Compare(object a, object b)
    {
        if (a is IComparable comparableA && b is IComparable comparableB)
        {
            return comparableA.CompareTo(Convert.ChangeType(comparableB, a.GetType()));
        }
        return 0;
    }

    private static bool IsIn(object value, object collection)
    {
        if (collection is IEnumerable<object> enumerable)
        {
            return enumerable.Contains(value);
        }
        return false;
    }

    private static bool Contains(object value, object search)
    {
        return value.ToString()?.Contains(search.ToString() ?? "") ?? false;
    }

    private static bool StartsWith(object value, object prefix)
    {
        return value.ToString()?.StartsWith(prefix.ToString() ?? "") ?? false;
    }

    private static bool IsBetween(object value, object range)
    {
        // Expects range as array [min, max]
        if (range is object[] arr && arr.Length == 2)
        {
            return Compare(value, arr[0]) >= 0 && Compare(value, arr[1]) <= 0;
        }
        return false;
    }
}
