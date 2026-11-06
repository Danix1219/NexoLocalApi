using Nexo.Discovery.Application.Feed;

namespace Nexo.Discovery.Unit;

public sealed class FeedCursorTests
{
    [Fact]
    public void Cursor_roundtrips_without_losing_the_identifier()
    {
        var original = new FeedCursor(new DateTime(2026, 11, 6, 12, 30, 0, DateTimeKind.Utc), "post-1");

        Assert.True(FeedCursor.TryDecode(original.Encode(), out var decoded));
        Assert.Equal(original.CreatedAt, decoded.CreatedAt);
        Assert.Equal(original.PostId, decoded.PostId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-base64")]
    [InlineData("c2luLXNlcGFyYWRvcg==")]
    public void Invalid_cursor_is_ignored_instead_of_failing(string? value)
    {
        Assert.False(FeedCursor.TryDecode(value, out _));
    }

    [Fact]
    public void Limit_is_clamped_to_the_contract_range()
    {
        Assert.Equal(FeedQuery.DefaultLimit, FeedQuery.Create(null, null, null, null, null, null).Limit);
        Assert.Equal(FeedQuery.MaxLimit, FeedQuery.Create(null, null, null, null, null, 500).Limit);
        Assert.Equal(1, FeedQuery.Create(null, null, null, null, null, 0).Limit);
    }

    [Fact]
    public void Blank_filters_are_normalized_to_null()
    {
        var query = FeedQuery.Create("  ", " centro ", "", " ", " cursor ", 10);

        Assert.Null(query.Mode);
        Assert.Equal("centro", query.Zone);
        Assert.Null(query.CategoryId);
        Assert.Null(query.Tab);
        Assert.Equal("cursor", query.Cursor);
    }
}
