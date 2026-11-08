using Nexo.Common.Results;

namespace Nexo.Common.Unit;

public sealed class PaginationTests
{
    [Fact]
    public void Defaults_to_twenty_and_accepts_the_maximum()
    {
        Assert.Equal(20, PageRequest.Create().Limit);
        Assert.Equal(50, PageRequest.Create(limit: 50).Limit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void Rejects_limits_outside_the_range(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageRequest.Create(limit: limit));
    }

    [Fact]
    public void Trims_cursor_and_treats_blank_as_first_page()
    {
        Assert.Null(PageRequest.Create("   ").AfterId);
        Assert.Equal("abc", PageRequest.Create("  abc  ").AfterId);
    }

    [Fact]
    public void Empty_page_has_no_cursor()
    {
        Assert.Empty(PagedResult<int>.Empty.Items);
        Assert.Null(PagedResult<int>.Empty.NextCursor);
        Assert.False(PagedResult<int>.Empty.HasMore);
    }
}
