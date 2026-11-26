using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Unit;

public sealed class ChatKeyTests
{
    private static string PublicKey()
    {
        var bytes = new byte[33]; bytes[0] = 5;
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes.AsSpan(1));
        return Convert.ToBase64String(bytes);
    }
    private static ChatDeviceKeys Valid() => new(1, 1234, PublicKey(), 1, PublicKey(),
        Convert.ToBase64String(new byte[64]), [new(1, PublicKey())],
        1, Convert.ToBase64String(new byte[1569]), Convert.ToBase64String(new byte[64]));

    [Fact]
    public void Public_bundle_requires_bounded_well_formed_material()
    {
        var keys = Valid();
        Assert.True(ChatKeyValidation.Valid(keys));
        Assert.False(ChatKeyValidation.Valid(keys with { DeviceId = 0 }));
        Assert.False(ChatKeyValidation.Valid(keys with { DeviceId = 6 }));
        Assert.False(ChatKeyValidation.Valid(keys with { RegistrationId = 0 }));
        Assert.False(ChatKeyValidation.Valid(keys with { IdentityKey = new string('A', 100000) }));
        Assert.False(ChatKeyValidation.Valid(keys with { IdentityKey = Convert.ToBase64String(new byte[33]) }));
        Assert.False(ChatKeyValidation.Valid(keys with { Signature = "not-base64" }));
        Assert.False(ChatKeyValidation.Valid(keys with { OneTimeKeys = [] }));
        Assert.False(ChatKeyValidation.Valid(keys with { OneTimeKeys = [new(1, PublicKey()), new(1, PublicKey())] }));
        Assert.False(ChatKeyValidation.Valid(keys with { OneTimeKeys = Enumerable.Range(0, 101).Select(i => new ChatPreKey(i, PublicKey())).ToArray() }));
    }
}
