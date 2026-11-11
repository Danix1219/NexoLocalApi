# Activar publicaciones reales

Diagnóstico 2026-11-10: el usuario confirma que todavía no existe un recurso Content Safety. El .env encontrado por el Host carece de ContentSafety__Endpoint y ContentSafety__ApiKey. El 503 observado es la protección prevista ante moderación no disponible; health/ready 200 solo acredita las dependencias generales, no la aprobación de texto. No se cambió ni se omitió esa protección.

1. Crear un recurso **Azure AI Content Safety** en la suscripción y región elegidas, revisando el nivel y sus condiciones en el portal antes de crearlo. Requiere autorización del titular; no se creó infraestructura en este ajuste.
2. Copiar su endpoint y una clave desde **Resource Management → Keys and Endpoint** al archivo .env que carga el backend (actualmente el situado en la raíz nexolocal_api). Nunca añadir la clave al repositorio ni al chat:

```dotenv
ContentSafety__Endpoint=<endpoint-https-del-recurso>
ContentSafety__ApiKey=<clave-del-recurso>
```

3. Desde NexoLocalApi ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check_content_safety.ps1`. El comando solo analiza una frase neutra, no crea solicitudes ni muestra claves. Sin configuración termina con código 2; un error externo o respuesta rechazada termina con 1; respuesta válida de las cuatro categorías produce CONTENT-SAFETY-OK.
4. Reiniciar la API para que lea las variables y reintentar el borrador de la app. Texto aprobado permite continuar con Mongo/cuota/transacción; rechazo de contenido devuelve 403. La subida de fotos sigue pendiente de almacenamiento/moderación/purga.

Guía oficial: [Microsoft: analizar texto con Content Safety](https://learn.microsoft.com/en-us/azure/ai-services/content-safety/quickstart-text). Contratos: [solicitudes](../specs/endpoints/06-solicitudes/implementacion.md) y [trabajos](../specs/endpoints/05-trabajos/implementacion.md). No presentar el chequeo sin claves como aceptación cloud.
