using FluentAssertions;
using Platform.BuildingBlocks.HashChain;
using Xunit;

namespace Platform.Tests.Unit;

public class CanonicalJsonTests
{
    [Fact]
    public void Serialize_ShouldSortKeysAlphabetically()
    {
        // Arrange
        var obj = new { zebra = "last", apple = "first", mango = "middle" };

        // Act
        var json = CanonicalJsonSerializer.Serialize(obj);

        // Assert
        json.Should().StartWith("{\"apple\":");
        json.IndexOf("\"apple\"").Should().BeLessThan(json.IndexOf("\"mango\""));
        json.IndexOf("\"mango\"").Should().BeLessThan(json.IndexOf("\"zebra\""));
    }

    [Fact]
    public void Serialize_ShouldExcludeNullValues()
    {
        // Arrange
        var obj = new { name = "test", nullValue = (string?)null, number = 42 };

        // Act
        var json = CanonicalJsonSerializer.Serialize(obj);

        // Assert
        json.Should().NotContain("nullValue");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"number\"");
    }

    [Fact]
    public void Serialize_ShouldProduceNoWhitespace()
    {
        // Arrange
        var obj = new { name = "test", nested = new { inner = "value" } };

        // Act
        var json = CanonicalJsonSerializer.Serialize(obj);

        // Assert
        json.Should().NotContain(" ");
        json.Should().NotContain("\n");
        json.Should().NotContain("\t");
    }

    [Fact]
    public void Serialize_ShouldBeDeterministic()
    {
        // Arrange
        var obj = new { id = 123, name = "test", amount = 99.99m };

        // Act
        var json1 = CanonicalJsonSerializer.Serialize(obj);
        var json2 = CanonicalJsonSerializer.Serialize(obj);

        // Assert
        json1.Should().Be(json2);
    }

    [Fact]
    public void Serialize_NestedObjects_ShouldSortKeysRecursively()
    {
        // Arrange
        var obj = new
        {
            outer = new { zebra = 1, apple = 2 },
            name = "test"
        };

        // Act
        var json = CanonicalJsonSerializer.Serialize(obj);

        // Assert
        json.Should().Contain("\"apple\":2");
        json.IndexOf("\"apple\"").Should().BeLessThan(json.IndexOf("\"zebra\""));
    }
}
