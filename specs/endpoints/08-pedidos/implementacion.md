# Registro de implementación local — 08 pedidos

Estado: **parcial; requiere configurar el App Service y completar los bloqueos indicados antes de aceptar los flujos de punta a punta**.

## Implementado

- Rutas HTTP autenticadas de crear/listar/consultar pedidos, aceptar/rechazar/cancelar/resolver y cambiar domicilio.
- Reglas de transición de estado, control de versión optimista y moderación de texto con Content Safety.
- Índices Mongo para pedido activo, idempotencia e historial; escrituras transaccionales de pedido, evento y outbox.
- Cifrado de sobre para domicilios: AES-256-GCM por registro y envoltura RSA-OAEP-256 de la clave de datos con Key Vault.
- Compatibilidad de lectura para ciphertext `v1` mediante la clave AES local existente, si hay registros previos que la requieran.
- Pruebas unitarias del flujo v2 con RSA local de prueba, lectura v1, transiciones y cifrado.

## Pendiente

1. **Configuración de Azure:** agregar `Orders__KeyVaultKeyId` al App Service `nexolocal` con el identificador HTTPS versionado de `orders-address-kek-dev`, guardar y reiniciar. La identidad administrada debe conservar `Key Vault Crypto User`. No se ha validado una operación real contra Azure ni se ha desplegado esta compilación.
2. **Asignación desde solicitud:** `origin=post` requiere leer una solicitud propia, verificar interés activo, asignar condicionalmente en Discovery y compensar el pedido si falla. `IRequestStore` todavía no incluye esa operación.
3. **Eventos derivados:** se persisten mensajes outbox; faltan consumidores para notificación, sincronización de estado de solicitud, invalidación y conversación. La API no declara esos efectos completados.
4. **Contrato y pruebas de seguridad:** falta alinear todas las respuestas/códigos con las fichas y ejecutar la suite de seguridad con Mongo y Docker disponibles.

## Validación local

- `dotnet test tests/Nexo.Engagement.Unit/Nexo.Engagement.Unit.csproj -c Release --no-restore -m:1 --nologo`: **18/18 aprobadas**.
- `dotnet build src/Host/Nexo.Host/Nexo.Host.csproj -c Release --no-restore -m:1 --nologo`: **compilación correcta, 0 advertencias y 0 errores**.

Esta evidencia es local; no acredita despliegue, acceso efectivo a Key Vault ni validación contra Atlas/Azure.
