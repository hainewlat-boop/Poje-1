using System.Security.Cryptography;
using System.Text;

namespace Platform.Security.Cryptography;

/// <summary>
/// Data encryption service for PII protection.
/// Uses AES-256-GCM (envelope encryption pattern).
/// </summary>
public interface IDataEncryption
{
    /// <summary>
    /// Encrypts data using the current data encryption key.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts data.
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts data with a specific key version for key rotation.
    /// </summary>
    string Encrypt(string plainText, string keyId);

    /// <summary>
    /// Gets the current key ID in use.
    /// </summary>
    string CurrentKeyId { get; }
}

/// <summary>
/// Implementation of AES-256-GCM encryption.
/// In production, keys should come from KMS/HSM.
/// </summary>
public class AesGcmDataEncryption : IDataEncryption
{
    private readonly IKeyManagementService _keyManagementService;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public AesGcmDataEncryption(IKeyManagementService keyManagementService)
    {
        _keyManagementService = keyManagementService;
    }

    public string CurrentKeyId => _keyManagementService.CurrentKeyId;

    public string Encrypt(string plainText)
    {
        return Encrypt(plainText, CurrentKeyId);
    }

    public string Encrypt(string plainText, string keyId)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var key = _keyManagementService.GetKey(keyId);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: keyId:nonce:tag:cipher (all base64)
        var keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        var result = new byte[4 + keyIdBytes.Length + NonceSize + TagSize + cipherBytes.Length];
        
        var offset = 0;
        BitConverter.GetBytes(keyIdBytes.Length).CopyTo(result, offset);
        offset += 4;
        keyIdBytes.CopyTo(result, offset);
        offset += keyIdBytes.Length;
        nonce.CopyTo(result, offset);
        offset += NonceSize;
        tag.CopyTo(result, offset);
        offset += TagSize;
        cipherBytes.CopyTo(result, offset);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var data = Convert.FromBase64String(cipherText);
        var offset = 0;

        // Read key ID
        var keyIdLength = BitConverter.ToInt32(data, offset);
        offset += 4;
        var keyId = Encoding.UTF8.GetString(data, offset, keyIdLength);
        offset += keyIdLength;

        // Read nonce
        var nonce = new byte[NonceSize];
        Array.Copy(data, offset, nonce, 0, NonceSize);
        offset += NonceSize;

        // Read tag
        var tag = new byte[TagSize];
        Array.Copy(data, offset, tag, 0, TagSize);
        offset += TagSize;

        // Read cipher
        var cipherBytes = new byte[data.Length - offset];
        Array.Copy(data, offset, cipherBytes, 0, cipherBytes.Length);

        // Decrypt
        var key = _keyManagementService.GetKey(keyId);
        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}

/// <summary>
/// Key management service interface.
/// In production, integrate with KMS/HSM.
/// </summary>
public interface IKeyManagementService
{
    string CurrentKeyId { get; }
    byte[] GetKey(string keyId);
    void RotateKey();
}

/// <summary>
/// File-based key management for development.
/// DO NOT use in production - use KMS/HSM instead.
/// </summary>
public class FileBasedKeyManagementService : IKeyManagementService
{
    private readonly Dictionary<string, byte[]> _keys = new();
    private string _currentKeyId;
    private const int KeySize = 32; // 256 bits

    public FileBasedKeyManagementService(string masterKeyBase64)
    {
        // Initialize with a default key for development
        _currentKeyId = "dev-key-1";
        _keys[_currentKeyId] = string.IsNullOrEmpty(masterKeyBase64)
            ? RandomNumberGenerator.GetBytes(KeySize)
            : Convert.FromBase64String(masterKeyBase64);
    }

    public string CurrentKeyId => _currentKeyId;

    public byte[] GetKey(string keyId)
    {
        if (_keys.TryGetValue(keyId, out var key))
        {
            return key;
        }

        throw new KeyNotFoundException($"Key not found: {keyId}");
    }

    public void RotateKey()
    {
        var newKeyId = $"dev-key-{_keys.Count + 1}";
        _keys[newKeyId] = RandomNumberGenerator.GetBytes(KeySize);
        _currentKeyId = newKeyId;
    }
}
