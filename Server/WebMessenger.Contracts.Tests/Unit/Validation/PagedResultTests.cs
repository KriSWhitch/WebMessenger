using WebMessenger.Contracts.Models;

namespace WebMessenger.Contracts.Tests.Unit.Validation;

/// <summary>
/// Tests for <see cref="PagedResult{T}"/> construction and default values.
/// </summary>
public class PagedResultTests
{
    [Fact]
    public void PagedResult_DefaultConstruction_ItemsIsEmpty()
    {
        // Arrange / Act
        var result = new PagedResult<string>();

        // Assert
        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Null(result.NextBefore);
    }

    [Fact]
    public void PagedResult_WithItems_ReturnsCorrectCount()
    {
        // Arrange / Act
        var result = new PagedResult<string>
        {
            Items    = ["a", "b", "c"],
            HasMore  = true,
            NextBefore = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.NotNull(result.NextBefore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void PagedResult_VariousItemCounts_ItemCountMatchesInput(int count)
    {
        // Arrange
        var items = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();

        // Act
        var result = new PagedResult<string> { Items = items };

        // Assert
        Assert.Equal(count, result.Items.Count);
    }
}
