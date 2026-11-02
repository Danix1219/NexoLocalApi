# Checklist de seguridad adaptado a Nexo

Estado: diseño de controles, sin certificación ni nivel ASVS/MASVS acreditado. El nombre conserva el patrón de docs vivos; mapear edición/control exactos al fijar estándar, no inventar conformidad.

- [ ] Autenticación, expiración, refresh y revocación en dos instancias.
- [ ] Autorización por recurso/estado y consentimiento; teléfono/domicilio cancelado no expuestos.
- [ ] Separación por cuenta: caché, SQLite, borradores, autoría y medios.
- [ ] Contraseñas/tokens/códigos/PII ausentes de logs y errores.
- [ ] Moderación previa, medios públicos/privados y rechazos sin persistencia de producto.
- [ ] Rate limits, validación, concurrencia e idempotencia.
- [ ] Borrado y retención/respaldos/proyecciones con evidencia.
- [ ] Permisos Android/deep links/almacenamiento seguro según superficie móvil.
- [ ] Headers/TLS/CORS para HTTP y landing; no copiar cookies/antiforgery Razor a Flutter.
