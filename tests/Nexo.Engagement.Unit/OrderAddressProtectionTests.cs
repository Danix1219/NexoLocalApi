using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Nexo.Engagement.Application.Orders;
using Nexo.Engagement.Infrastructure;

namespace Nexo.Engagement.Unit;

public sealed class OrderAddressProtectionTests
{
    private static OrderAddressProtection Protector(string? key) => new(new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["Orders:AddressEncryptionKeyBase64"] = key }).Build());

    [Fact]
    public async Task Cifra_domicilio_referencias_y_coordenadas_con_nonce_no_repetido()
    {
        var key = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray());
        var protector = Protector(key); var value = new OrderAddress("Calle Hidalgo 123", "Portón verde", new(20.0539, -99.3403));
        var first = await protector.EncryptAsync(value, default); var second = await protector.EncryptAsync(value, default);
        Assert.NotEqual(first, second);
        Assert.Equal(value, await protector.DecryptAsync(first, default));
        Assert.DoesNotContain(value.Address, first);
        Assert.DoesNotContain(value.Note!, first);
    }

    [Fact]
    public async Task No_cifra_si_no_hay_clave_de_32_bytes()
    {
        await Assert.ThrowsAsync<OrderUnavailableException>(() => Protector(null).EncryptAsync(new("Casa", null, new(20, -99)), default));
        var badKey = Convert.ToBase64String(new byte[16]);
        await Assert.ThrowsAsync<OrderUnavailableException>(() => Protector(badKey).EncryptAsync(new("Casa", null, new(20, -99)), default));
    }

    [Fact]
    public async Task Sobre_v2_usa_el_id_versionado_y_conserva_la_direccion_cifrada()
    {
        using var wrapper = new RsaTestKeyWrapper();
        const string keyId = "https://nexolocal-kv-dev.vault.azure.net/keys/orders-address-kek-dev/0123456789abcdef";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orders:KeyVaultKeyId"] = keyId
        }).Build();
        var protector = new OrderAddressProtection(config, wrapper);
        var value = new OrderAddress("Calle Hidalgo 123", "Portón verde", new(20.0539, -99.3403));

        var encrypted = await protector.EncryptAsync(value, default);

        Assert.StartsWith("v2.", encrypted);
        Assert.DoesNotContain(value.Address, encrypted);
        Assert.Equal(value, await protector.DecryptAsync(encrypted, default));
        Assert.Equal(keyId, wrapper.LastKeyId);
    }

    [Fact]
    public async Task Sigue_descifrando_el_formato_v1_existente_al_migrar_a_Key_Vault()
    {
        var key = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray());
        var oldProtector = Protector(key);
        var value = new OrderAddress("Calle Hidalgo 123", null, new(20.0539, -99.3403));
        var oldCipher = await oldProtector.EncryptAsync(value, default);
        var newConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orders:KeyVaultKeyId"] = "https://nexolocal-kv-dev.vault.azure.net/keys/orders-address-kek-dev/0123456789abcdef",
            ["Orders:AddressEncryptionKeyBase64"] = key
        }).Build();
        using var wrapper = new RsaTestKeyWrapper();
        var migrated = new OrderAddressProtection(newConfig, wrapper);

        Assert.Equal(value, await migrated.DecryptAsync(oldCipher, default));
    }

    private sealed class RsaTestKeyWrapper : IOrderAddressKeyWrapper, IDisposable
    {
        private readonly RSA _rsa = RSA.Create(3072);
        public string? LastKeyId { get; private set; }
        public Task<byte[]> WrapAsync(string keyId, byte[] key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); LastKeyId = keyId;
            return Task.FromResult(_rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256));
        }
        public Task<byte[]> UnwrapAsync(string keyId, byte[] wrappedKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); LastKeyId = keyId;
            return Task.FromResult(_rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256));
        }
        public void Dispose() => _rsa.Dispose();
    }
}
