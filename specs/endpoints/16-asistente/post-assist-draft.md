# Borrador con IA generativa · `POST /v1/assist/draft` (EP-082)

| Campo | Valor |
|---|---|
| Módulo | Discovery (dueño de solicitudes y trabajos) |
| Quién | Cuenta autenticada |
| Consumidor | App, spec 049 (`assist_sheet.dart`, botón «Ayúdame a redactar») |
| Idempotente | No aplica: no persiste nada |

## Función
Propone un borrador **editable** de una solicitud (`request`) o de un trabajo (`work`) a partir de lo que la persona
escribió. Nunca publica: el texto pasa al formulario solo con «Usar este texto» y se publica con el botón de siempre.

## Petición
```json
{ "kind": "request", "categoryId": "plomeria", "userText": "se me rompió la llave del lavabo", "lengthLimit": 600 }
```
| Campo | Regla |
|---|---|
| `kind` | `request` \| `work` |
| `categoryId` | slug `^[a-z0-9_-]{2,30}$` |
| `userText` | 3–1000 caracteres |
| `lengthLimit` | 100–1000; por defecto 600 |

## Respuesta `200`
```json
{ "draft": { "text": "…", "title": "…" }, "modelVersion": "gpt-4o-mini" }
```

## Errores
| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Campos fuera de regla |
| 401 | — | Sin sesión |
| 403 | `content.inappropriate` | La entrada no pasa Content Safety (incluida la lista de groserías) |
| 429 | `rate.limit_exceeded` | Más de 20 borradores al día por cuenta |
| 503 | `assist.unavailable` | Sin configuración, el modelo falla, tarda más de 20 s o su salida no pasa la moderación |

## Seguridad y privacidad
1. Teléfonos, correos y enlaces se quitan en la app **y otra vez en el servidor**, de la entrada y de la salida.
2. Se moderan la entrada (antes de gastar cupo o llamar al modelo) y la salida. Una salida rechazada es `503`, no culpa de la persona.
3. **No se guarda el texto**: solo un contador por cuenta y día (`assist_usage`, TTL 2 días).
4. El prompt prohíbe inventar precios, fechas, marcas, direcciones o teléfonos.

## Tecnología
Azure OpenAI (chat completions por REST, `api-version 2024-10-21`, respuesta JSON). Configuración:
`AzureOpenAI__Endpoint`, `AzureOpenAI__ApiKey`, `AzureOpenAI__Deployment`. Vacías = el asistente responde `503`.
