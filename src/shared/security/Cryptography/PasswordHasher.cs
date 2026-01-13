using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;

namespace Platform.Security.Cryptography;

/// <summary>
/// Password hashing using Argon2id with bcrypt fallback.
/// Argon2id is the recommended algorithm for password hashing.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Implementation of password hasher using Argon2id.
/// Parameters: Memory 64MB, Iterations 3, Parallelism 4
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 4;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing, // Argon2id
            Version = Argon2Version.Nineteen,
            MemoryCost = MemorySize,
            TimeCost = Iterations,
            Lanes = Parallelism,
            Threads = Parallelism,
            Salt = salt,
            Password = System.Text.Encoding.UTF8.GetBytes(password),
            HashLength = HashSize
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        
        return config.EncodeString(hash.Buffer);
    }

    /// <summary>
    /// Verifies a password against an Argon2 hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        try
        {
            return Argon2.Verify(hash, password);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// BCrypt fallback for legacy password verification.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Composite password hasher that supports both Argon2id and BCrypt.
/// Uses Argon2id for new hashes, supports BCrypt for migration.
/// </summary>
public class CompositePasswordHasher : IPasswordHasher
{
    private readonly Argon2PasswordHasher _argon2Hasher;
    private readonly BcryptPasswordHasher _bcryptHasher;

    public CompositePasswordHasher()
    {
        _argon2Hasher = new Argon2PasswordHasher();
        _bcryptHasher = new BcryptPasswordHasher();
    }

    public string HashPassword(string password)
    {
        // Always use Argon2id for new hashes
        return _argon2Hasher.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Detect hash type and verify accordingly
        if (hash.StartsWith("$argon2"))
        {
            return _argon2Hasher.VerifyPassword(password, hash);
        }
        
        if (hash.StartsWith("$2"))
        {
            return _bcryptHasher.VerifyPassword(password, hash);
        }

        return false;
    }

    /// <summary>
    /// Checks if a hash needs to be upgraded to Argon2id.
    /// </summary>
    public bool NeedsUpgrade(string hash)
    {
        return !hash.StartsWith("$argon2");
    }
}
