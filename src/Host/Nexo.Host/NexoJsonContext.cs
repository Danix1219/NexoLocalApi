using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Nexo.Host;

/// <summary>
/// Serializadores generados para los cuerpos HTTP publicos; evita reflexion en el camino caliente.
/// Sin DTO de dominio: las operaciones anteriores se retiraron por estar mal elegidas.
/// Cada cuerpo nuevo se añade aqui con su `JsonSerializable` al definir su endpoint.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class NexoJsonContext : JsonSerializerContext
{
}
