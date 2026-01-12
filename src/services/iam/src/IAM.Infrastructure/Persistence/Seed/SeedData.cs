using IAM.Domain.Entities;
using IAM.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Security.Cryptography;

namespace IAM.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds initial data for IAM service.
/// </summary>
public class SeedData
{
    private readonly IamDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SeedData> _logger;

    public SeedData(
        IamDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<SeedData> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedPoliciesAsync();
        await SeedAdminUserAsync();
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Database seeding completed");
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync())
        {
            return;
        }

        var roles = new[]
        {
            CreateRole("admin", "System Administrator", true,
                "users:read", "users:write", "users:delete",
                "roles:read", "roles:write",
                "policies:read", "policies:write",
                "applications:read", "applications:write", "applications:approve",
                "payments:read", "payments:write", "payments:refund",
                "audit:read", "audit:export",
                "documents:read", "documents:write"),
            
            CreateRole("operator", "Application Operator", true,
                "users:read",
                "applications:read", "applications:write", "applications:approve",
                "payments:read",
                "documents:read", "documents:write"),
            
            CreateRole("reviewer", "Application Reviewer", true,
                "applications:read", "applications:approve",
                "documents:read"),
            
            CreateRole("citizen", "Citizen User", true,
                "applications:read:own", "applications:write:own",
                "payments:read:own", "payments:write:own",
                "documents:read:own"),
            
            CreateRole("auditor", "Audit Viewer", true,
                "audit:read", "audit:export",
                "applications:read",
                "payments:read",
                "documents:read")
        };

        await _context.Roles.AddRangeAsync(roles);
        _logger.LogInformation("Seeded {Count} roles", roles.Length);
    }

    private static Role CreateRole(string name, string description, bool isSystem, params string[] permissions)
    {
        var role = Role.Create(name, description, isSystem);
        foreach (var permission in permissions)
        {
            role.AddPermission(permission);
        }
        return role;
    }

    private async Task SeedPoliciesAsync()
    {
        if (await _context.Policies.AnyAsync())
        {
            return;
        }

        var policies = new[]
        {
            // Payment policies require OTPT
            Policy.Create("payment:create:policy", "payment:intent", "create", PolicyEffect.Allow, 100, false, true),
            Policy.Create("payment:refund:policy", "payment:transaction", "refund", PolicyEffect.Allow, 100, true, true),
            
            // Application approval requires OTPT
            Policy.Create("application:approve:policy", "application:task", "complete", PolicyEffect.Allow, 100, false, true),
            
            // User management requires MFA
            Policy.Create("user:create:policy", "user", "create", PolicyEffect.Allow, 100, true, false),
            Policy.Create("user:delete:policy", "user", "delete", PolicyEffect.Allow, 100, true, true),
            
            // Audit export requires MFA and OTPT
            Policy.Create("audit:export:policy", "audit", "export", PolicyEffect.Allow, 100, true, true)
        };

        await _context.Policies.AddRangeAsync(policies);
        _logger.LogInformation("Seeded {Count} policies", policies.Length);
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
        if (adminRole == null)
        {
            _logger.LogWarning("Admin role not found, cannot seed admin user");
            return;
        }

        var email = Email.Create("admin@platform.gov.tr");
        var name = PersonName.Create("System", "Administrator");
        var passwordHash = _passwordHasher.HashPassword("Admin123!@#$");

        var adminUser = User.Create(email, name, passwordHash);
        adminUser.AssignRole(adminRole);

        await _context.Users.AddAsync(adminUser);
        _logger.LogInformation("Seeded admin user: {Email}", email.Value);
    }
}
