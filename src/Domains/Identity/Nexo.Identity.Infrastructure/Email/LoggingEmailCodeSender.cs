using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Verification;

namespace Nexo.Identity.Infrastructure.Email;

/// <summary>
/// Envio de codigo por correo sin proveedor ratificado (D04 sigue abierta).
/// shortcut: fuera de Development no entrega nada y lo deja anotado; sustituir por el proveedor real al ratificar D04.
/// </summary>
public sealed class LoggingEmailCodeSender(IHostEnvironment environment, ILogger<LoggingEmailCodeSender> logger) : IEmailCodeSender
{
    public Task SendAsync(string email, string code, ChallengePurpose purpose, CancellationToken ct)
    {
        if (environment.IsDevelopment())
        {
            // Solo en desarrollo: sin proveedor de correo no hay otra forma de completar el flujo.
            logger.LogWarning("Codigo de {Purpose} para {Email}: {Code} (solo desarrollo)", purpose, email, code);
        }
        else
        {
            logger.LogError(
                "Codigo de {Purpose} para {Email} no entregado: falta proveedor de correo ratificado (D04).",
                purpose,
                email);
        }

        return Task.CompletedTask;
    }
}
