using FluentValidation;
using IAM.Domain.Entities;
using IAM.Domain.Repositories;
using IAM.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Domain;
using Platform.BuildingBlocks.Result;
using Platform.Contracts.IAM;
using Platform.Security.Cryptography;

namespace IAM.Application.Commands;

/// <summary>
/// Command to create a new user.
/// </summary>
public record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? NationalId,
    string? PhoneNumber,
    IReadOnlyList<string> Roles) : IRequest<Result<UserResponse>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{10,15}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDataEncryption _dataEncryption;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IDataEncryption dataEncryption,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _dataEncryption = dataEncryption;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create email value object
        var email = Email.Create(request.Email);

        // Check if email already exists
        if (await _userRepository.ExistsWithEmailAsync(email, cancellationToken))
        {
            _logger.LogWarning("Attempt to create user with existing email: {Email}", request.Email);
            return Result.Failure<UserResponse>(
                Error.ConflictError("User.EmailExists", "A user with this email already exists"));
        }

        // Create person name value object
        var name = PersonName.Create(request.FirstName, request.LastName);

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Encrypt national ID if provided
        EncryptedValue? encryptedNationalId = null;
        if (!string.IsNullOrEmpty(request.NationalId))
        {
            var encrypted = _dataEncryption.Encrypt(request.NationalId);
            encryptedNationalId = EncryptedValue.Create(encrypted, _dataEncryption.CurrentKeyId);
        }

        // Create phone number if provided
        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        }

        // Create user
        var user = User.Create(email, name, passwordHash, encryptedNationalId, phoneNumber);

        // Assign roles
        foreach (var roleName in request.Roles)
        {
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role != null)
            {
                user.AssignRole(role);
            }
            else
            {
                _logger.LogWarning("Role not found: {RoleName}", roleName);
            }
        }

        // Save user
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {UserId} ({Email})", user.Id, user.Email.Value);

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
            Roles = request.Roles
        });
    }
}
