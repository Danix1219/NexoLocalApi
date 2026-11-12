using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Infrastructure;

/// <summary>Key-encryption-key operations. The data-encryption key never leaves this process unwrapped.</summary>
public interface IOrderAddressKeyWrapper
{
    Task<byte[]> WrapAsync(string keyId, byte[] key, CancellationToken ct);
    Task<byte[]> UnwrapAsync(string keyId, byte[] wrappedKey, CancellationToken ct);
}

/// <summary>Uses the Key Vault RSA key to wrap per-record AES keys.</summary>
public sealed class AzureOrderAddressKeyWrapper : IOrderAddressKeyWrapper
{
    private readonly DefaultAzureCredential _credential = new();
    private readonly ConcurrentDictionary<string, CryptographyClient> _clients = new(StringComparer.Ordinal);

    public async Task<byte[]> WrapAsync(string keyId, byte[] key, CancellationToken ct)
    {
        var result = await Client(keyId).WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, key, ct);
        return result.EncryptedKey;
    }

    public async Task<byte[]> UnwrapAsync(string keyId, byte[] wrappedKey, CancellationToken ct)
    {
        var result = await Client(keyId).UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, wrappedKey, ct);
        return result.Key;
    }

    private CryptographyClient Client(string keyId) => _clients.GetOrAdd(keyId, id =>
        new CryptographyClient(ValidateKeyId(id), _credential));

    private static Uri ValidateKeyId(string keyId)
    {
        if (!Uri.TryCreate(keyId, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.Segments.Length < 4 || uri.Segments[1] != "keys/" ||
            string.IsNullOrWhiteSpace(uri.Segments[2]) || string.IsNullOrWhiteSpace(uri.Segments[3]))
            throw new InvalidOperationException("Orders Key Vault key identifier must be a versioned HTTPS key URI.");
        return uri;
    }
}

/// <summary>
/// Protects private order addresses. v2 uses envelope encryption: AES-256-GCM for the payload and
/// Azure Key Vault RSA-OAEP-256 for the per-record data key. v1 remains readable for migration.
/// </summary>
public sealed class OrderAddressProtection : IOrderAddressProtection
{
    private readonly IConfiguration _configuration;
    private readonly IOrderAddressKeyWrapper? _keyWrapper;

    public OrderAddressProtection(IConfiguration configuration, IOrderAddressKeyWrapper? keyWrapper = null)
    {
        _configuration = configuration;
        _keyWrapper = keyWrapper;
    }

    public async Task<string> EncryptAsync(OrderAddress address, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var keyId = _configuration["Orders:KeyVaultKeyId"];
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            if (_keyWrapper is null) throw new OrderUnavailableException();
            var dek = RandomNumberGenerator.GetBytes(32);
            var plain = Serialize(address);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            try
            {
                using (var aes = new AesGcm(dek, tag.Length)) aes.Encrypt(nonce, plain, cipher, tag);
                var wrapped = await _keyWrapper.WrapAsync(keyId, dek, ct);
                var envelope = new Envelope(keyId, Convert.ToBase64String(wrapped), Convert.ToBase64String(nonce),
                    Convert.ToBase64String(tag), Convert.ToBase64String(cipher));
                return "v2." + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(envelope));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { throw new OrderUnavailableException(e); }
            finally
            {
                CryptographicOperations.ZeroMemory(dek);
                CryptographicOperations.ZeroMemory(plain);
                CryptographicOperations.ZeroMemory(cipher);
            }
        }

        // Local development and migration path. In Azure set Orders__KeyVaultKeyId instead.
        var localKey = LocalKey();
        var localPlain = Serialize(address);
        var localNonce = RandomNumberGenerator.GetBytes(12);
        var localCipher = new byte[localPlain.Length];
        var localTag = new byte[16];
        try
        {
            using var aes = new AesGcm(localKey, localTag.Length);
            aes.Encrypt(localNonce, localPlain, localCipher, localTag);
            return "v1." + Convert.ToBase64String([.. localNonce, .. localTag, .. localCipher]);
        }
        catch (CryptographicException e) { throw new OrderUnavailableException(e); }
        finally
        {
            CryptographicOperations.ZeroMemory(localKey);
            CryptographicOperations.ZeroMemory(localPlain);
            CryptographicOperations.ZeroMemory(localCipher);
        }
    }

    public async Task<OrderAddress> DecryptAsync(string cipher, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (cipher.StartsWith("v1.", StringComparison.Ordinal)) return DecryptV1(cipher[3..]);
        if (!cipher.StartsWith("v2.", StringComparison.Ordinal) || _keyWrapper is null)
            throw new OrderUnavailableException(new CryptographicException("Unsupported address cipher version."));

        byte[]? dek = null;
        byte[]? plain = null;
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(Convert.FromBase64String(cipher[3..]))
                ?? throw new CryptographicException("Address envelope is invalid.");
            if (string.IsNullOrWhiteSpace(envelope.KeyId)) throw new CryptographicException("Address envelope key id is missing.");
            var wrapped = Convert.FromBase64String(envelope.WrappedKey);
            var nonce = Convert.FromBase64String(envelope.Nonce);
            var tag = Convert.FromBase64String(envelope.Tag);
            var encrypted = Convert.FromBase64String(envelope.Ciphertext);
            if (nonce.Length != 12 || tag.Length != 16) throw new CryptographicException("Address envelope has invalid AES-GCM parameters.");
            dek = await _keyWrapper.UnwrapAsync(envelope.KeyId, wrapped, ct);
            if (dek.Length != 32) throw new CryptographicException("Unwrapped address key has an invalid size.");
            plain = new byte[encrypted.Length];
            using (var aes = new AesGcm(dek, tag.Length)) aes.Decrypt(nonce, encrypted, tag, plain);
            return Deserialize(plain);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { throw new OrderUnavailableException(e); }
        finally
        {
            if (dek is not null) CryptographicOperations.ZeroMemory(dek);
            if (plain is not null) CryptographicOperations.ZeroMemory(plain);
        }
    }

    private OrderAddress DecryptV1(string encoded)
    {
        var key = LocalKey();
        byte[]? plain = null;
        try
        {
            var packed = Convert.FromBase64String(encoded);
            if (packed.Length < 28) throw new CryptographicException("Cipher payload is truncated.");
            plain = new byte[packed.Length - 28];
            using (var aes = new AesGcm(key, 16)) aes.Decrypt(packed.AsSpan(0, 12), packed.AsSpan(28), packed.AsSpan(12, 16), plain);
            return Deserialize(plain);
        }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException or InvalidOperationException)
        {
            throw new OrderUnavailableException(e);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            if (plain is not null) CryptographicOperations.ZeroMemory(plain);
        }
    }

    private byte[] LocalKey()
    {
        var raw = _configuration["Orders:AddressEncryptionKeyBase64"];
        if (string.IsNullOrWhiteSpace(raw)) throw new OrderUnavailableException(new InvalidOperationException("Orders address encryption key is not configured."));
        try
        {
            var key = Convert.FromBase64String(raw);
            if (key.Length != 32) throw new FormatException("AES-256 requires a 32-byte key.");
            return key;
        }
        catch (FormatException e) { throw new OrderUnavailableException(e); }
    }

    private static byte[] Serialize(OrderAddress address) => JsonSerializer.SerializeToUtf8Bytes(
        new Payload(address.Address, address.Note, address.Point.Lat, address.Point.Lng));

    private static OrderAddress Deserialize(byte[] plain)
    {
        var value = JsonSerializer.Deserialize<Payload>(plain) ?? throw new CryptographicException("Address payload is invalid.");
        return new OrderAddress(value.Address, value.Note, new(value.Lat, value.Lng));
    }

    private sealed record Payload(string Address, string? Note, double Lat, double Lng);
    private sealed record Envelope(string KeyId, string WrappedKey, string Nonce, string Tag, string Ciphertext);
}
