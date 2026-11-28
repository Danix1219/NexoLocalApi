# Elegibilidad para opinar · `GET /v1/engagement/internal/orders/{id}/review-eligibility`

| Campo | Valor |
|---|---|
| Módulo | Engagement (Orders) |
| Quién | Participante del pedido, con su propio token. Uso interno: la consume el flujo de opiniones, no una pantalla |
| Spec SDD | nv-10 ([task](../../arquitectura/nv-10/task.md)) |
| Caché | `private, no-store` |

## Función

Dice si la cuenta puede dejar opinión sobre el pedido: participa en él y el pedido terminó. Evita que
Opiniones lea directamente la base de Pedidos (frontera entre módulos).

## Respuesta 200

```json
{ "participant": true, "resolved": true, "providerId": "0194…" }
```

| Estado | Cuándo |
|---|---|
| 200 | El pedido existe y la cuenta participa |
| 401 | Sin sesión |
| 404 `resource.not_found` | No existe o la cuenta no participa (no revela cuál) |
| 503 | Pedidos no disponible |
