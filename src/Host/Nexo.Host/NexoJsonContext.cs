using System.Text.Json.Serialization;
using Nexo.Identity.Endpoints.Http;

namespace Nexo.Host;

/// <summary>Serializadores generados para los cuerpos HTTP publicos; evita reflexion en el camino caliente.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RequestCodeBody))]
[JsonSerializable(typeof(VerifyCodeBody))]
[JsonSerializable(typeof(RegisterBody))]
[JsonSerializable(typeof(LoginBody))]
[JsonSerializable(typeof(RefreshBody))]
[JsonSerializable(typeof(PasswordResetBody))]
[JsonSerializable(typeof(ChallengeResponse))]
[JsonSerializable(typeof(ProofResponse))]
[JsonSerializable(typeof(SessionResponse))]
[JsonSerializable(typeof(RegisteredResponse))]
[JsonSerializable(typeof(MeResponse))]
internal sealed partial class NexoJsonContext : JsonSerializerContext
{
}
