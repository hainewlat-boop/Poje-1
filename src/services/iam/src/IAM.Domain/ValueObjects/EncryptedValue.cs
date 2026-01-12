using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.ValueObjects;

/// <summary>
/// Value object representing an encrypted value (e.g., National ID).
/// The actual value is encrypted at rest.
/// </summary>
public sealed class EncryptedValue : ValueObject
{
    private EncryptedValue(string encryptedData, string keyId)
    {
        EncryptedData = encryptedData;
        KeyId = keyId;
    }

    /// <summary>
    /// The encrypted data (base64 encoded).
    /// </summary>
    public string EncryptedData { get; }

    /// <summary>
    /// The key ID used for encryption (for key rotation support).
    /// </summary>
    public string KeyId { get; }

    public static EncryptedValue Create(string encryptedData, string keyId)
    {
        if (string.IsNullOrWhiteSpace(encryptedData))
        {
            throw new ArgumentException("Encrypted data cannot be empty", nameof(encryptedData));
        }

        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("Key ID cannot be empty", nameof(keyId));
        }

        return new EncryptedValue(encryptedData, keyId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EncryptedData;
        yield return KeyId;
    }
}
