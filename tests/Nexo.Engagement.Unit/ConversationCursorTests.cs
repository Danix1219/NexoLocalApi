using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Unit;

public sealed class ConversationCursorTests
{
    [Fact]
    public void Cursor_de_bandeja_ida_y_vuelta()
    {
        var at = new DateTime(2026, 11, 5, 19, 30, 0, DateTimeKind.Utc);
        var c = new Conversation(Guid.CreateVersion7(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, at, at, null, null, 0, 0, []);
        var cursor = ConversationService.EncodeCursor(c);
        Assert.True(cursor.Length <= 64);
        Assert.Equal((at, c.Id), ConversationService.DecodeCursor(cursor));
    }

    [Theory]
    [InlineData("basura")]
    [InlineData("-5_00000000000000000000000000000000")]
    [InlineData("123_no-es-guid")]
    public void Cursor_invalido_se_rechaza(string cursor) => Assert.Null(ConversationService.DecodeCursor(cursor));
}
