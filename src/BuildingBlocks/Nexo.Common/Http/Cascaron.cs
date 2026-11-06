using Microsoft.AspNetCore.Http;

namespace Nexo.Common.Http;

/// <summary>
/// Respuesta de las rutas del cascaron: la ruta existe y esta registrada, pero su caso de uso
/// todavia no se implementa. Devuelve 501 con Problem Details (RFC 9457) y el codigo del
/// endpoint, para que el consumidor distinga "no existe" (404) de "aun no esta hecho" (501).
/// </summary>
public static class Cascaron
{
    public static IResult PendienteDeImplementar(string ep, string spec) => Microsoft.AspNetCore.Http.Results.Problem(
        title: "Operacion no implementada todavia",
        detail: $"{ep} esta declarado en el catalogo y su ruta esta montada, pero su caso de uso no se ha implementado. Spec: {spec}.",
        statusCode: StatusCodes.Status501NotImplemented,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "NO_IMPLEMENTADO",
            ["ep"] = ep,
            ["spec"] = spec
        });
}
