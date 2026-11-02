# Políticas de consumo

Estado: diseño pendiente de fijar valores por operación. No se copian automáticamente los límites Login/Admin del ejemplo.

| Frontera | Regla | Estado |
|---|---|---|
| OTP/verificación/reenvío | Límites por IP/teléfono/desafío, expiración e intentos atómicos | Propuestas de identidad; contrato por formalizar |
| Login/refresh | Anti-enumeración, cuota, rotación y replay | Diseño pendiente de pruebas reales |
| Escrituras/publicaciones | Idempotencia, moderación y límite por usuario | Pendiente |
| Lecturas/listas | Cursor y máximo por contrato; caché pública sin PII | Pendiente |
| Generación/medios | Tamaño/tipo/cuota/costo, rechazo si dependencia falla | Pendiente |
| GraphQL opcional | Operaciones registradas, profundidad/costo y timeout | Condicionado a S08 |

Flutter presenta 429/Retry-After y reintentos acotados; solo el backend impone la política autoritativa. No describir rate-limit distribuido como implementado.
