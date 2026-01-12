using FluentAssertions;
using Platform.BuildingBlocks.HashChain;
using Xunit;

namespace Platform.Tests.Unit;

public class HashChainTests
{
    private readonly IHashChainService _hashChainService;

    public HashChainTests()
    {
        _hashChainService = new HashChainService();
    }

    [Fact]
    public void ComputeHash_ShouldGenerateDeterministicHash()
    {
        // Arrange
        var record = new TestRecord { Id = "123", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var previousHash = "genesis-hash";

        // Act
        var hash1 = _hashChainService.ComputeHash(record, previousHash);
        var hash2 = _hashChainService.ComputeHash(record, previousHash);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2, "Hash should be deterministic");
    }

    [Fact]
    public void ComputeHash_DifferentRecords_ShouldGenerateDifferentHashes()
    {
        // Arrange
        var record1 = new TestRecord { Id = "123", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var record2 = new TestRecord { Id = "124", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var previousHash = "genesis-hash";

        // Act
        var hash1 = _hashChainService.ComputeHash(record1, previousHash);
        var hash2 = _hashChainService.ComputeHash(record2, previousHash);

        // Assert
        hash1.Should().NotBe(hash2, "Different records should produce different hashes");
    }

    [Fact]
    public void ComputeHash_DifferentPreviousHash_ShouldGenerateDifferentHashes()
    {
        // Arrange
        var record = new TestRecord { Id = "123", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var previousHash1 = "genesis-hash";
        var previousHash2 = "different-genesis-hash";

        // Act
        var hash1 = _hashChainService.ComputeHash(record, previousHash1);
        var hash2 = _hashChainService.ComputeHash(record, previousHash2);

        // Assert
        hash1.Should().NotBe(hash2, "Different previous hashes should produce different hashes");
    }

    [Fact]
    public void VerifyHash_ValidHash_ShouldReturnTrue()
    {
        // Arrange
        var record = new TestRecord { Id = "123", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var previousHash = "genesis-hash";
        var computedHash = _hashChainService.ComputeHash(record, previousHash);

        // Act
        var isValid = _hashChainService.VerifyHash(record, previousHash, computedHash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHash_TamperedRecord_ShouldReturnFalse()
    {
        // Arrange
        var record = new TestRecord { Id = "123", Amount = 100.50m, Timestamp = "2024-01-15T10:00:00.000Z" };
        var previousHash = "genesis-hash";
        var computedHash = _hashChainService.ComputeHash(record, previousHash);

        // Tamper with record
        var tamperedRecord = new TestRecord { Id = "123", Amount = 200.00m, Timestamp = "2024-01-15T10:00:00.000Z" };

        // Act
        var isValid = _hashChainService.VerifyHash(tamperedRecord, previousHash, computedHash);

        // Assert
        isValid.Should().BeFalse("Tampered record should not verify");
    }

    [Fact]
    public void GenerateGenesisHash_ShouldGenerateValidHash()
    {
        // Arrange & Act
        var genesisHash = _hashChainService.GenerateGenesisHash("production", "1.0.0");

        // Assert
        genesisHash.Should().NotBeNullOrEmpty();
        genesisHash.Should().HaveLength(64, "SHA-256 produces 64 hex characters");
    }

    private class TestRecord
    {
        public string Id { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Timestamp { get; set; } = null!;
    }
}
