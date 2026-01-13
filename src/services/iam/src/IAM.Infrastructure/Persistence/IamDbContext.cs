using IAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Domain;
using Platform.BuildingBlocks.Outbox;

namespace IAM.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for IAM service.
/// </summary>
public class IamDbContext : DbContext, IUnitOfWork
{
    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyCondition> PolicyConditions => Set<PolicyCondition>();
    public DbSet<PolicyRole> PolicyRoles => Set<PolicyRole>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam_schema");

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .IsConcurrencyToken();

            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("email")
                    .HasMaxLength(256)
                    .IsRequired();
                
                email.HasIndex(e => e.Value).IsUnique();
            });

            entity.OwnsOne(e => e.Name, name =>
            {
                name.Property(n => n.FirstName)
                    .HasColumnName("first_name")
                    .HasMaxLength(100)
                    .IsRequired();
                
                name.Property(n => n.LastName)
                    .HasColumnName("last_name")
                    .HasMaxLength(100)
                    .IsRequired();
            });

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(500)
                .IsRequired();

            entity.OwnsOne(e => e.NationalId, nationalId =>
            {
                nationalId.Property(n => n.EncryptedData)
                    .HasColumnName("national_id_encrypted")
                    .HasMaxLength(500);
                
                nationalId.Property(n => n.KeyId)
                    .HasColumnName("national_id_key_id")
                    .HasMaxLength(50);
            });

            entity.OwnsOne(e => e.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("phone_number")
                    .HasMaxLength(20);
            });

            entity.Property(e => e.MfaEnabled)
                .HasColumnName("mfa_enabled")
                .IsRequired();

            entity.Property(e => e.MfaSecret)
                .HasColumnName("mfa_secret")
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");

            entity.Property(e => e.LockedUntil)
                .HasColumnName("locked_until");

            entity.Property(e => e.FailedLoginAttempts)
                .HasColumnName("failed_login_attempts")
                .IsRequired();

            entity.HasMany(e => e.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.DomainEvents);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.IsSystemRole)
                .HasColumnName("is_system_role")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasMany(e => e.Permissions)
                .WithOne()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.RoleId)
                .HasColumnName("role_id")
                .IsRequired();

            entity.Property(e => e.AssignedAt)
                .HasColumnName("assigned_at")
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.RoleId)
                .HasColumnName("role_id")
                .IsRequired();

            entity.Property(e => e.Permission)
                .HasColumnName("permission")
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(e => new { e.RoleId, e.Permission }).IsUnique();
        });

        // Policy configuration
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("policies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Resource)
                .HasColumnName("resource")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasColumnName("action")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Effect)
                .HasColumnName("effect")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Priority)
                .HasColumnName("priority")
                .IsRequired();

            entity.Property(e => e.RequiresMfa)
                .HasColumnName("requires_mfa")
                .IsRequired();

            entity.Property(e => e.RequiresOtpt)
                .HasColumnName("requires_otpt")
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasMany(e => e.Conditions)
                .WithOne()
                .HasForeignKey(pc => pc.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Resource, e.Action });
        });

        // PolicyCondition configuration
        modelBuilder.Entity<PolicyCondition>(entity =>
        {
            entity.ToTable("policy_conditions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PolicyId)
                .HasColumnName("policy_id")
                .IsRequired();

            entity.Property(e => e.Attribute)
                .HasColumnName("attribute")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Operator)
                .HasColumnName("operator")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Value)
                .HasColumnName("value")
                .HasMaxLength(500)
                .IsRequired();
        });

        // PolicyRole configuration
        modelBuilder.Entity<PolicyRole>(entity =>
        {
            entity.ToTable("policy_roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PolicyId)
                .HasColumnName("policy_id")
                .IsRequired();

            entity.Property(e => e.RoleName)
                .HasColumnName("role_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => new { e.PolicyId, e.RoleName }).IsUnique();
        });

        // OutboxMessage configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");

            entity.Property(e => e.Error)
                .HasColumnName("error")
                .HasMaxLength(2000);

            entity.Property(e => e.RetryCount)
                .HasColumnName("retry_count")
                .IsRequired();

            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
