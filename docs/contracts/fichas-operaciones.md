# Cobertura de operaciones — revisión 0.4.0

Vista documental generada del [inventario](inventario.json), autoridad única de rutas/semántica. No es OpenAPI ni segunda autoridad de payloads. Inputs/resultados se transcriben como diseño de la línea base; tipos exactos/nulos/enums/errores/compatibilidad se formalizan antes de implementar cada historia. Cambiar inventario y regenerar esta vista si cambia el diseño.

## EP-001 — POST /auth/otp

- Propietario: S01; actor: Público limitado; superficie: pública/autenticada según actor.
- Objetivo: Solicitar verificación o recuperación del teléfono.
- Entradas de diseño: phoneE164, purpose registration/recovery.
- Resultado de diseño: 202 challengeId y retryAfter.
- Invariante: Respuesta neutral; límites por IP/teléfono; no devolver código
- Referencia funcional del catálogo: `obresend, aunext`; entidad: `otp_challenges`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-201 → T-201 → AC-201.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-002 — POST /auth/otp/verify

- Propietario: S01; actor: Público con desafío; superficie: pública/autenticada según actor.
- Objetivo: Comprobar código y obtener prueba de control.
- Entradas de diseño: challengeId, code.
- Resultado de diseño: 200 proofToken de un uso y corta duración.
- Invariante: Verificar intentos, propósito, expiración y no reutilización
- Referencia funcional del catálogo: `obnext, aunext`; entidad: `otp_challenges`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-202 → T-202 → AC-202.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-003 — POST /auth/register

- Propietario: S01; actor: Público con prueba OTP; superficie: pública/autenticada según actor.
- Objetivo: Crear cuenta única y consentimiento.
- Entradas de diseño: proofToken, password, displayName, consentVersions.
- Resultado de diseño: 201 account y sesión.
- Invariante: Teléfono único; nombre moderado; secreto protegido
- Referencia funcional del catálogo: `obFinish`; entidad: `accounts`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-203 → T-203 → AC-203.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-004 — POST /auth/login

- Propietario: S01; actor: Público limitado; superficie: pública/autenticada según actor.
- Objetivo: Iniciar sesión con teléfono y contraseña.
- Entradas de diseño: phoneE164, password, deviceId.
- Resultado de diseño: 200 accessToken y refreshToken.
- Invariante: Error neutral; hash adaptativo; nunca aceptar cuentas demo en release
- Referencia funcional del catálogo: `aunext`; entidad: `sessions`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-204 → T-204 → AC-204.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-005 — POST /auth/refresh

- Propietario: S01; actor: Poseedor de refresh; superficie: pública/autenticada según actor.
- Objetivo: Renovar sesión sin login repetido.
- Entradas de diseño: refreshToken, deviceId.
- Resultado de diseño: 200 nuevo par de tokens.
- Invariante: Rotación atómica; replay revoca familia; coordinación de peticiones móviles
- Referencia funcional del catálogo: `Sesión persistente requerida`; entidad: `sessions`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-205 → T-205 → AC-205.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-006 — POST /auth/password-reset

- Propietario: S01; actor: Prueba OTP recovery; superficie: pública/autenticada según actor.
- Objetivo: Restablecer acceso y revocar sesiones previas.
- Entradas de diseño: proofToken, newPassword.
- Resultado de diseño: 204.
- Invariante: No aceptar prueba de registro ni código ya consumido
- Referencia funcional del catálogo: `aunext newpass`; entidad: `accounts`.
- Spec primaria: [nv-16](../../specs/arquitectura/nv-16/spec.md); FR-206 → T-206 → AC-206.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [017-cuenta-recuperacion-sesiones](../../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-007 — POST /auth/logout

- Propietario: S01; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Cerrar sesión actual en backend y dispositivos.
- Entradas de diseño: sid derivado del JWT.
- Resultado de diseño: 204.
- Invariante: Revocación durable y Redis; cierre WebSocket; operación idempotente
- Referencia funcional del catálogo: `logoutdo`; entidad: `sessions`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-207 → T-207 → AC-207.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-008 — GET /me

- Propietario: S01; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Recuperar cuenta, capacidades y preferencias.
- Entradas de diseño: JWT.
- Resultado de diseño: 200 perfil privado y modos autorizados; summary de solicitudes/experiencias/guardados mediante composición autorizada.
- Invariante: El modo de UI no concede privilegios
- Referencia funcional del catálogo: `V.account`; entidad: `accounts`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-208 → T-208 → AC-208.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [004-identidad-sesion](../../../App_NexoLocal/specs/004-identidad-sesion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-009 — PATCH /me

- Propietario: S01; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Editar nombre, intereses y preferencias.
- Entradas de diseño: Campos permitidos, If-Match, photoMediaId, coverMediaId, privateHomeLocation/publicZone.
- Resultado de diseño: 200 cuenta y nueva version.
- Invariante: Lista blanca; moderar nombre; no permitir cambio libre de roles; fotos propias aprobadas; origen privado nunca se publica.
- Referencia funcional del catálogo: `uint, chan, mode`; entidad: `accounts`.
- Spec primaria: [nv-16](../../specs/arquitectura/nv-16/spec.md); FR-209 → T-209 → AC-209.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [017-cuenta-recuperacion-sesiones](../../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-010 — GET /me/sessions

- Propietario: S01; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Consultar sesiones de sus dispositivos.
- Entradas de diseño: cursor, limit.
- Resultado de diseño: 200 lista de sesiones sin secretos.
- Invariante: No devolver refreshHash ni direcciones completas
- Referencia funcional del catálogo: `Ampliación de seguridad`; entidad: `sessions`.
- Spec primaria: [nv-16](../../specs/arquitectura/nv-16/spec.md); FR-210 → T-210 → AC-210.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [017-cuenta-recuperacion-sesiones](../../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-011 — DELETE /me/sessions/{sid}

- Propietario: S01; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Revocar otro dispositivo.
- Entradas de diseño: sid.
- Resultado de diseño: 204.
- Invariante: Verificar dueño de sesión antes de revocar
- Referencia funcional del catálogo: `Ampliación de seguridad`; entidad: `sessions`.
- Spec primaria: [nv-16](../../specs/arquitectura/nv-16/spec.md); FR-211 → T-211 → AC-211.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [017-cuenta-recuperacion-sesiones](../../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-012 — POST /me/deletion

- Propietario: S01; actor: Propietario reautenticado; superficie: pública/autenticada según actor.
- Objetivo: Solicitar eliminación integral.
- Entradas de diseño: reauthProof, confirmation.
- Resultado de diseño: 202 deletionId y estado.
- Invariante: Bloquear cuenta y emitir evento; no afirmar completado al recibir
- Referencia funcional del catálogo: `delconfirm`; entidad: `deletions`.
- Spec primaria: [nv-12](../../specs/arquitectura/nv-12/spec.md); FR-212 → T-212 → AC-212.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [012-notificaciones-borrado](../../../App_NexoLocal/specs/012-notificaciones-borrado/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-013 — GET /me/deletion/{id}

- Propietario: S01; actor: Credencial limitada al trámite; superficie: pública/autenticada según actor.
- Objetivo: Consultar progreso del borrado.
- Entradas de diseño: id, deletionReceipt.
- Resultado de diseño: 200 estado y servicios pendientes.
- Invariante: Receipt solo permite consultar este trámite, nunca reabrir cuenta
- Referencia funcional del catálogo: `Requisito 16`; entidad: `deletions`.
- Spec primaria: [nv-12](../../specs/arquitectura/nv-12/spec.md); FR-213 → T-213 → AC-213.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [012-notificaciones-borrado](../../../App_NexoLocal/specs/012-notificaciones-borrado/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-014 — GET /catalog/categories

- Propietario: S02; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Listar categorías, especialidades y sectores.
- Entradas de diseño: version opcional.
- Resultado de diseño: 200 catálogo y ETag.
- Invariante: Excluir altas inactivas; caché público 24 h
- Referencia funcional del catálogo: `CATS, SPEC, obStep`; entidad: `categories`.
- Spec primaria: [nv-17](../../specs/arquitectura/nv-17/spec.md); FR-214 → T-214 → AC-214.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [018-catalogo-seleccion](../../../App_NexoLocal/specs/018-catalogo-seleccion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-015 — PUT /admin/catalog/categories/{id}

- Propietario: S02; actor: Administrador; superficie: administrativa.
- Objetivo: Crear o actualizar categoría.
- Entradas de diseño: label, synonyms, active, If-Match si existe.
- Resultado de diseño: 200/201 categoría.
- Invariante: Auditar; conservar referencias; invalidar caché
- Referencia funcional del catálogo: `Operación requerida`; entidad: `categories`.
- Spec primaria: [nv-17](../../specs/arquitectura/nv-17/spec.md); FR-215 → T-215 → AC-215.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-016 — POST /providers

- Propietario: S02; actor: Cuenta sin perfil; superficie: pública/autenticada según actor.
- Objetivo: Crear perfil de persona o negocio.
- Entradas de diseño: type, name, categorías, modalidad, ubicación, radio, mediaIds.
- Resultado de diseño: 201 perfil privado/publicable.
- Invariante: Un perfil por cuenta; mínimos de registro; texto/medios aprobados; portafolio de registro hasta seis fotos aprobadas; no contar fotos como trabajos ni reutilizar providerId=me entre cuentas.
- Referencia funcional del catálogo: `buildProvider, obFinish`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-216 → T-216 → AC-216.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-017 — GET /providers/{id}

- Propietario: S02; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Consultar perfil público y señales.
- Entradas de diseño: id.
- Resultado de diseño: 200 DTO público y ETag; conteos separados de publicaciones, fotos y pedidos resueltos.
- Invariante: Ocultar origen privado y datos de cuenta; teléfono según permisos
- Referencia funcional del catálogo: `V.profile`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-217 → T-217 → AC-217.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-018 — GET /me/provider

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Consultar perfil editable y completitud.
- Entradas de diseño: JWT.
- Resultado de diseño: 200 perfil privado; completeness versionada y opportunitySummary con conteos distintos.
- Invariante: Solo propio; completitud se calcula desde campos persistidos
- Referencia funcional del catálogo: `profileTodo`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-218 → T-218 → AC-218.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-019 — PATCH /me/provider

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Actualizar datos comerciales y servicios.
- Entradas de diseño: Campos permitidos, If-Match; responsable, experiencia, equipo/materiales, factura declarada, web/sector y medios de perfil.
- Resultado de diseño: 200 perfil y versión.
- Invariante: Máximo tres precios orientativos en UI inicial; catálogo/categorías aparte; contenido moderado; editor permanente permite corregir campos completos, no solo faltantes.
- Referencia funcional del catálogo: `svcadd, edsave, addcat`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-219 → T-219 → AC-219.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-020 — PATCH /me/provider/coverage

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Editar modalidad y zona de atención.
- Entradas de diseño: mode, privateOrigin, radiusKm, publicBusinessLocation, If-Match.
- Resultado de diseño: 200 cobertura.
- Invariante: Validar rango de coordenadas y radio positivo en dom/ambos, cero permitido en local; publicación comercial explícita
- Referencia funcional del catálogo: `obloc, addrad`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-220 → T-220 → AC-220.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-021 — PATCH /me/provider/schedule

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Configurar horarios y urgencias.
- Entradas de diseño: schedule, timeZone, urgent, If-Match.
- Resultado de diseño: 200 horario.
- Invariante: Cierre posterior a apertura para alcance inicial; validar zona IANA
- Referencia funcional del catálogo: `edday, edurg, edsave`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-221 → T-221 → AC-221.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-022 — PATCH /me/provider/prices

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Definir visita y precios orientativos.
- Entradas de diseño: visitPrice opcional, priceItems 0..3 con label, amountMinor, currency, unit, pricingMode, serviceKey opcional e includesMaterials, If-Match.
- Resultado de diseño: 200 condiciones.
- Invariante: Cero representa visita gratis; ausencia no equivale a cero; tres filas visibles; no comparar montos de unidades distintas.
- Referencia funcional del catálogo: `edvm, edsave`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-222 → T-222 → AC-222.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-023 — POST /me/provider/refresh

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Confirmar vigencia de información.
- Entradas de diseño: If-Match.
- Resultado de diseño: 200 updatedAt y versión.
- Invariante: No aumentar reputación por pulsar actualizar
- Referencia funcional del catálogo: `refresh`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-223 → T-223 → AC-223.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-024 — DELETE /me/provider

- Propietario: S02; actor: Prestador reautenticado; superficie: pública/autenticada según actor.
- Objetivo: Retirar perfil de publicación.
- Entradas de diseño: confirmation.
- Resultado de diseño: 202 retiro.
- Invariante: Ampliación operativa; definir tratamiento de pedidos abiertos y vínculos
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `providers`.
- Spec primaria: [nv-06](../../specs/arquitectura/nv-06/spec.md); FR-224 → T-224 → AC-224.
- Estado: Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada. formalContract sigue null.
- Consumidor móvil primario: [006-perfiles](../../../App_NexoLocal/specs/006-perfiles/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-025 — GET /providers

- Propietario: S02; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Buscar y filtrar prestadores por necesidad y zona.
- Entradas de diseño: q, category, lat, lng, radius, mode, open, freeVisit, hasPrices, urgent, sort, cursor, limit.
- Resultado de diseño: 200 resultados, nextCursor, coverageStatus.
- Invariante: Cobertura real por prestador; orden estable; ubicación exacta no sale
- Referencia funcional del catálogo: `V.search, resultsHTML, filt, fsort`; entidad: `providers`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-225 → T-225 → AC-225.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-026 — GET /search/suggestions

- Propietario: S02; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Autocompletar servicios y términos reconocidos.
- Entradas de diseño: q mínimo 2 caracteres, limit máximo 10.
- Resultado de diseño: 200 sugerencias.
- Invariante: Tokens/prefijos indexados; debounce móvil; excluir búsqueda privada histórica
- Referencia funcional del catálogo: `sugShow, qsug`; entidad: `categories`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-226 → T-226 → AC-226.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-027 — GET /providers/compare

- Propietario: S02; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Comparar exactamente dos prestadores.
- Entradas de diseño: ids de dos perfiles.
- Resultado de diseño: 200 columnas y priceComparison {comparable,reason,serviceKey,unit,currency}.
- Invariante: Evitar inventar precio u horario faltante; route estática antes de {id}; destacar precio menor solo si servicio, moneda, unidad y materiales/alcance coinciden; de lo contrario no comparable y sin MEJOR.
- Referencia funcional del catálogo: `V.compare, cmp`; entidad: `providers`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-227 → T-227 → AC-227.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-028 — GET /geo/search

- Propietario: S02; actor: Público limitado; superficie: pública/autenticada según actor.
- Objetivo: Buscar lugar para seleccionar ubicación.
- Entradas de diseño: q, country MX, bounds, limit.
- Resultado de diseño: 200 lugares y coordenadas.
- Invariante: Adaptador Azure Maps; comprobar licencia/cupos; limitar precisión pública
- Referencia funcional del catálogo: `mapfind`; entidad: `read_cache`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-228 → T-228 → AC-228.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-029 — GET /geo/reverse

- Propietario: S02; actor: Público limitado; superficie: pública/autenticada según actor.
- Objetivo: Obtener etiqueta de zona desde punto.
- Entradas de diseño: lat, lng.
- Resultado de diseño: 200 etiqueta y zona pública.
- Invariante: No guardar consultas precisas en logs o caché pública sin anonimizar
- Referencia funcional del catálogo: `pkLabel`; entidad: `read_cache`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-229 → T-229 → AC-229.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-030 — GET /feed

- Propietario: S03; actor: Público o cuenta según filtro; superficie: pública/autenticada según actor.
- Objetivo: Componer inicio por zona y modo.
- Entradas de diseño: mode, zone, category, tab, cursor, limit.
- Resultado de diseño: 200 tarjetas y cursor.
- Invariante: Gateway agrega DTO públicos; para mine exige usuario; fallos parciales explícitos; conservar trabajo cercano por cobertura de prestador o distancia del trabajo <=3 km según postNear, usando ubicación privada solo dentro del servidor.
- Referencia funcional del catálogo: `V.home, htab`; entidad: `posts`.
- Spec primaria: [nv-07](../../specs/arquitectura/nv-07/spec.md); FR-230 → T-230 → AC-230.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [007-descubrimiento](../../../App_NexoLocal/specs/007-descubrimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-031 — POST /posts

- Propietario: S03; actor: Prestador; superficie: pública/autenticada según actor.
- Objetivo: Publicar un trabajo realizado.
- Entradas de diseño: workDone 10..200, categoryId, publicZone, privateWorkLocation opcional, mediaIds 0..4.
- Resultado de diseño: 201 publicación.
- Invariante: Categoría ofrecida, 0..4 medios propios aprobados; título automático hasta 60 caracteres. No exigir campos residuales problema/tiempo.
- Referencia funcional del catálogo: `publish`; entidad: `posts`.
- Spec primaria: [nv-08](../../specs/arquitectura/nv-08/spec.md); FR-231 → T-231 → AC-231.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [008-comunidad](../../../App_NexoLocal/specs/008-comunidad/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-032 — GET /posts

- Propietario: S03; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Consultar trabajos filtrados y perfil.
- Entradas de diseño: providerId, categoryId, zone, cursor, limit.
- Resultado de diseño: 200 lista.
- Invariante: Solo publicados; orden por createdAt y _id
- Referencia funcional del catálogo: `V.home, V.profile work`; entidad: `posts`.
- Spec primaria: [nv-08](../../specs/arquitectura/nv-08/spec.md); FR-232 → T-232 → AC-232.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [008-comunidad](../../../App_NexoLocal/specs/008-comunidad/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-033 — GET /posts/{id}

- Propietario: S03; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Abrir detalle de trabajo.
- Entradas de diseño: id.
- Resultado de diseño: 200 trabajo.
- Invariante: No exponer domicilio donde se realizó
- Referencia funcional del catálogo: `workSheet`; entidad: `posts`.
- Spec primaria: [nv-08](../../specs/arquitectura/nv-08/spec.md); FR-233 → T-233 → AC-233.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [008-comunidad](../../../App_NexoLocal/specs/008-comunidad/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-034 — PATCH /posts/{id}

- Propietario: S03; actor: Autor; superficie: pública/autenticada según actor.
- Objetivo: Corregir contenido de trabajo.
- Entradas de diseño: Campos editables, If-Match.
- Resultado de diseño: 200 publicación actualizada.
- Invariante: Volver a moderar cambios; conservar versión anterior solo según política
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `posts`.
- Spec primaria: [nv-08](../../specs/arquitectura/nv-08/spec.md); FR-234 → T-234 → AC-234.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [008-comunidad](../../../App_NexoLocal/specs/008-comunidad/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-035 — DELETE /posts/{id}

- Propietario: S03; actor: Autor o moderador; superficie: pública/autenticada según actor.
- Objetivo: Retirar trabajo.
- Entradas de diseño: id, If-Match.
- Resultado de diseño: 204.
- Invariante: Ocultar feed e invalidar caché; purgar medios que queden sin referencias
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `posts`.
- Spec primaria: [nv-08](../../specs/arquitectura/nv-08/spec.md); FR-235 → T-235 → AC-235.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [008-comunidad](../../../App_NexoLocal/specs/008-comunidad/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-036 — PUT /reactions/{type}/{id}

- Propietario: S03; actor: Cuenta; superficie: pública/autenticada según actor.
- Objetivo: Recomendar trabajo o perfil.
- Entradas de diseño: type, id.
- Resultado de diseño: 200 estado recomendado.
- Invariante: Una reacción por cuenta/objetivo; tipo validado; no contador libre
- Referencia funcional del catálogo: `rec`; entidad: `saved`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-236 → T-236 → AC-236.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-037 — DELETE /reactions/{type}/{id}

- Propietario: S03; actor: Autor de reacción; superficie: pública/autenticada según actor.
- Objetivo: Retirar recomendación.
- Entradas de diseño: type, id.
- Resultado de diseño: 204.
- Invariante: Idempotente; recalcular agregados por evento
- Referencia funcional del catálogo: `rec`; entidad: `saved`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-237 → T-237 → AC-237.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-038 — GET /me/saved

- Propietario: S03; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Consultar prestadores, trabajos o solicitudes guardados.
- Entradas de diseño: type, cursor, limit.
- Resultado de diseño: 200 lista.
- Invariante: Autorización al hidratar cada recurso; mostrar retirado cuando aplique
- Referencia funcional del catálogo: `V.saved`; entidad: `saved`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-238 → T-238 → AC-238.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-039 — PUT /me/saved/{type}/{id}

- Propietario: S03; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Guardar objetivo.
- Entradas de diseño: type provider/post/request, id.
- Resultado de diseño: 201 o 200.
- Invariante: Restricción única; verificar visibilidad del objetivo
- Referencia funcional del catálogo: `savep, savew, saver`; entidad: `saved`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-239 → T-239 → AC-239.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-040 — DELETE /me/saved/{type}/{id}

- Propietario: S03; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Quitar guardado.
- Entradas de diseño: type, id.
- Resultado de diseño: 204.
- Invariante: Idempotente y sincronizable offline
- Referencia funcional del catálogo: `savep, savew, saver`; entidad: `saved`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-240 → T-240 → AC-240.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-041 — POST /reviews

- Propietario: S03; actor: Cuenta; superficie: pública/autenticada según actor.
- Objetivo: Publicar experiencia o calificación.
- Entradas de diseño: providerId obligatorio, text 10..280, recommends booleano, stars entero 1..5, orderId opcional.
- Resultado de diseño: 201 experiencia con tipo de evidencia.
- Invariante: Pedido done propio para registeredOrder; no autoevaluación; verificar prestador activo, un solo destinatario y campos obligatorios también en servidor; outbox review.upserted con versión.
- Referencia funcional del catálogo: `publishexp, mreview`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-241 → T-241 → AC-241.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-042 — GET /providers/{id}/reviews

- Propietario: S03; actor: Público; superficie: pública/autenticada según actor.
- Objetivo: Consultar opiniones y resumen.
- Entradas de diseño: id, kind opcional, cursor, limit.
- Resultado de diseño: 200 experiencias y agregados.
- Invariante: Distinguir declarado de pedido registrado; no inflar contadores
- Referencia funcional del catálogo: `V.profile trust`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-242 → T-242 → AC-242.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-043 — PATCH /reviews/{id}

- Propietario: S03; actor: Autor; superficie: pública/autenticada según actor.
- Objetivo: Corregir experiencia.
- Entradas de diseño: text 10..280, recommends, stars 1..5, providerId solo para declared, If-Match.
- Resultado de diseño: 200 experiencia.
- Invariante: Moderar de nuevo y recalcular agregados; no cambiar autor/pedido; en registeredOrder providerId queda ligado al pedido; snapshot reemplaza relaciones de mención/recomendación anteriores.
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-243 → T-243 → AC-243.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-044 — DELETE /reviews/{id}

- Propietario: S03; actor: Autor o moderador; superficie: pública/autenticada según actor.
- Objetivo: Retirar experiencia.
- Entradas de diseño: id, If-Match.
- Resultado de diseño: 204.
- Invariante: Actualizar promedio y recomendaciones; auditar retiro administrativo
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-244 → T-244 → AC-244.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-045 — POST /interactions

- Propietario: S03; actor: Público limitado o cuenta; superficie: pública/autenticada según actor.
- Objetivo: Registrar vista, compartir o intención de contacto.
- Entradas de diseño: eventId, providerId, kind.
- Resultado de diseño: 202.
- Invariante: Minimizar datos; idempotente; no equiparar clic con servicio realizado
- Referencia funcional del catálogo: `share, contact, stats`; entidad: `metrics`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-245 → T-245 → AC-245.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-046 — GET /me/provider/metrics

- Propietario: S03; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Consultar actividad de su perfil.
- Entradas de diseño: from, to, máximo 90 días.
- Resultado de diseño: 200 agregados.
- Invariante: Sin identidad de visitantes ni datos privados
- Referencia funcional del catálogo: `V.account stats`; entidad: `metrics`.
- Spec primaria: [nv-18](../../specs/arquitectura/nv-18/spec.md); FR-246 → T-246 → AC-246.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [019-guardados-indicadores](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-047 — POST /requests

- Propietario: S04; actor: Solicitante; superficie: pública/autenticada según actor.
- Objetivo: Publicar necesidad de servicio.
- Entradas de diseño: categoryId, description, privateLocation, publicZone, urgency, contactPreference.
- Resultado de diseño: 201 solicitud.
- Invariante: Texto moderado mínimo diez caracteres; no mostrar teléfono público; máximo 280 caracteres; contactPreference chat/both.
- Referencia funcional del catálogo: `publishreq`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-247 → T-247 → AC-247.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-048 — GET /requests

- Propietario: S04; actor: Prestador o visitante público limitado; superficie: pública/autenticada según actor.
- Objetivo: Consultar solicitudes de zona y capacidad.
- Entradas de diseño: categoryId, zone, ownerOnly, matchMyServices, withinMyCoverage, urgency=Hoy, includeOutsideCoverage explícito, cursor, limit.
- Resultado de diseño: 200 DTO sin domicilio/teléfono.
- Invariante: ownerOnly requiere sesión; matchMyServices compara categorías del perfil sin confundir propiedad. urgency=Hoy filtra urgencia, no antigüedad. Respuesta fuera de cobertura etiquetada y por ampliación explícita. Nunca domicilio ni teléfono público.
- Referencia funcional del catálogo: `V.search pro, rfilt`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-248 → T-248 → AC-248.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-049 — GET /requests/{id}

- Propietario: S04; actor: Público o propietario según campos; superficie: pública/autenticada según actor.
- Objetivo: Abrir solicitud.
- Entradas de diseño: id.
- Resultado de diseño: 200 versión pública o propia.
- Invariante: Propietario obtiene sus datos privados; otros solo zona
- Referencia funcional del catálogo: `reqopen, reqHTML`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-249 → T-249 → AC-249.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-050 — PATCH /requests/{id}

- Propietario: S04; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Corregir necesidad abierta.
- Entradas de diseño: description, urgency, categoryId, location, If-Match, contactPreference chat/both.
- Resultado de diseño: 200 solicitud.
- Invariante: Solo abierta sin asignación; moderación de texto modificado; nunca ampliar acceso a teléfono de pedido fuera de la autorización del cliente.
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-250 → T-250 → AC-250.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-051 — POST /requests/{id}/close

- Propietario: S04; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Cerrar necesidad ya resuelta.
- Entradas de diseño: If-Match.
- Resultado de diseño: 200 estado closed.
- Invariante: Si hay pedido ok, exigir resolver/cancelar antes para no contradecirlo
- Referencia funcional del catálogo: `closereq`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-251 → T-251 → AC-251.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-052 — DELETE /requests/{id}

- Propietario: S04; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Retirar solicitud.
- Entradas de diseño: id, If-Match.
- Resultado de diseño: 204.
- Invariante: No borrar un pedido activo en cascada; ocultar y auditar
- Referencia funcional del catálogo: `Complemento CRUD`; entidad: `requests`.
- Spec primaria: [nv-09](../../specs/arquitectura/nv-09/spec.md); FR-252 → T-252 → AC-252.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [009-solicitudes](../../../App_NexoLocal/specs/009-solicitudes/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-053 — PUT /requests/{id}/interest

- Propietario: S04; actor: Prestador; superficie: pública/autenticada según actor.
- Objetivo: Expresar interés.
- Entradas de diseño: id.
- Resultado de diseño: 200 interés.
- Invariante: Solicitud abierta ajena y perfil activo; una relación única; no impedir por categoría no coincidente: la maqueta permite esa acción; mantener visibilidad y estado como restricciones.
- Referencia funcional del catálogo: `reqint`; entidad: `interests`.
- Spec primaria: [nv-20](../../specs/arquitectura/nv-20/spec.md); FR-253 → T-253 → AC-253.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [021-interesados-candidatos](../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-054 — DELETE /requests/{id}/interest

- Propietario: S04; actor: Prestador interesado; superficie: pública/autenticada según actor.
- Objetivo: Retirar interés.
- Entradas de diseño: id.
- Resultado de diseño: 204.
- Invariante: No alterar asignación existente; retirar relación propia
- Referencia funcional del catálogo: `reqint`; entidad: `interests`.
- Spec primaria: [nv-20](../../specs/arquitectura/nv-20/spec.md); FR-254 → T-254 → AC-254.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [021-interesados-candidatos](../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-055 — GET /requests/{id}/interests

- Propietario: S04; actor: Dueño de solicitud; superficie: pública/autenticada según actor.
- Objetivo: Consultar interesados y ordenar.
- Entradas de diseño: sort rating/recommendations/open, cursor, limit.
- Resultado de diseño: 200 candidatos.
- Invariante: Enriquecer por lote con S02/S03 y mantener cursor estable
- Referencia funcional del catálogo: `V.ints, intsort`; entidad: `interests`.
- Spec primaria: [nv-20](../../specs/arquitectura/nv-20/spec.md); FR-255 → T-255 → AC-255.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [021-interesados-candidatos](../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-056 — PUT /requests/{id}/shortlist/{providerId}

- Propietario: S04; actor: Dueño de solicitud; superficie: pública/autenticada según actor.
- Objetivo: [Ampliación opcional] Preseleccionar candidato.
- Entradas de diseño: id, providerId.
- Resultado de diseño: 200 preselección.
- Invariante: Requiere interés existente; no equivale a aceptación
- Referencia funcional del catálogo: `Ampliación opcional: reqshort residual, sin control visible en T31`; entidad: `interests`.
- Spec primaria: [nv-20](../../specs/arquitectura/nv-20/spec.md); FR-256 → T-256 → AC-256.
- Estado: Opcional; no bloquea alcance obligatorio si no se habilita. formalContract sigue null.
- Consumidor móvil primario: [021-interesados-candidatos](../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-057 — DELETE /requests/{id}/shortlist/{providerId}

- Propietario: S04; actor: Dueño de solicitud; superficie: pública/autenticada según actor.
- Objetivo: [Ampliación opcional] Quitar preselección.
- Entradas de diseño: id, providerId.
- Resultado de diseño: 204.
- Invariante: Solo dueño; operación idempotente
- Referencia funcional del catálogo: `Ampliación opcional: reqshort residual, sin control visible en T31`; entidad: `interests`.
- Spec primaria: [nv-20](../../specs/arquitectura/nv-20/spec.md); FR-257 → T-257 → AC-257.
- Estado: Opcional; no bloquea alcance obligatorio si no se habilita. formalContract sigue null.
- Consumidor móvil primario: [021-interesados-candidatos](../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-058 — POST /orders

- Propietario: S04; actor: Solicitante; superficie: pública/autenticada según actor.
- Objetivo: Enviar pedido desde perfil, chat o interesado.
- Entradas de diseño: providerId, requestId opcional, categoryId, description, mode, privatePlace, clientContactPreference opcional.
- Resultado de diseño: 201 pedido pend.
- Invariante: Idempotency-Key; impedir activos duplicados; verificar interés si origin=post; heredar de solicitud; directo chat por defecto y both solo por consentimiento explícito.
- Referencia funcional del catálogo: `agok, reqpick, chatag`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-258 → T-258 → AC-258.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-059 — GET /me/orders

- Propietario: S04; actor: Participante; superficie: pública/autenticada según actor.
- Objetivo: Consultar pedidos por modo y estado.
- Entradas de diseño: mode, status, cursor, limit.
- Resultado de diseño: 200 lista propia.
- Invariante: Filtrar clientId o providerId derivado de sesión
- Referencia funcional del catálogo: `matchCard, V.home`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-259 → T-259 → AC-259.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-060 — GET /orders/{id}

- Propietario: S04; actor: Participante; superficie: pública/autenticada según actor.
- Objetivo: Consultar estado y logística autorizada.
- Entradas de diseño: id.
- Resultado de diseño: 200 pedido filtrado por actor.
- Invariante: Domicilio solo según estado y actor; no basta ocultarlo en Flutter
- Referencia funcional del catálogo: `V.match`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-260 → T-260 → AC-260.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-061 — POST /orders/{id}/accept

- Propietario: S04; actor: Prestador destinatario; superficie: pública/autenticada según actor.
- Objetivo: Aceptar y opcionalmente fijar precio.
- Entradas de diseño: quote opcional, If-Match, Idempotency-Key.
- Resultado de diseño: 200 pedido ok.
- Invariante: Transacción pedido+solicitud+outbox; solo pend; dirección después de commit
- Referencia funcional del catálogo: `mquoteok, mAccept`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-261 → T-261 → AC-261.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-062 — POST /orders/{id}/reject

- Propietario: S04; actor: Prestador destinatario; superficie: pública/autenticada según actor.
- Objetivo: Rechazar pedido pendiente.
- Entradas de diseño: If-Match.
- Resultado de diseño: 200 pedido cancel.
- Invariante: Liberar referencia de solicitud y retirar acceso privado
- Referencia funcional del catálogo: `mrejectok`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-262 → T-262 → AC-262.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-063 — POST /orders/{id}/cancel

- Propietario: S04; actor: Cliente o prestador participante; superficie: pública/autenticada según actor.
- Objetivo: Cancelar servicio activo.
- Entradas de diseño: reason opcional, If-Match.
- Resultado de diseño: 200 pedido cancel.
- Invariante: Solo pend/ok; invalidar autorización de domicilio y emitir evento
- Referencia funcional del catálogo: `mcancelok`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-263 → T-263 → AC-263.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-064 — POST /orders/{id}/complete

- Propietario: S04; actor: Prestador destinatario; superficie: pública/autenticada según actor.
- Objetivo: Declarar servicio resuelto.
- Entradas de diseño: If-Match.
- Resultado de diseño: 200 pedido done.
- Invariante: Solo ok; cerrar solicitud y habilitar calificación del cliente
- Referencia funcional del catálogo: `mdoneok`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-264 → T-264 → AC-264.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-065 — PATCH /orders/{id}/address

- Propietario: S04; actor: Cliente del pedido; superficie: pública/autenticada según actor.
- Objetivo: Cambiar punto y referencias del servicio.
- Entradas de diseño: privatePlace, If-Match.
- Resultado de diseño: 200 pedido.
- Invariante: Solo pend/ok; notificar cambio; no aceptar direcciones ajenas en evento público
- Referencia funcional del catálogo: `maddr`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-265 → T-265 → AC-265.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [010-pedidos](../../../App_NexoLocal/specs/010-pedidos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-066 — POST /conversations

- Propietario: S05; actor: Cliente o prestador habilitado; superficie: pública/autenticada según actor.
- Objetivo: Abrir o recuperar conversación por pareja.
- Entradas de diseño: providerId o orderId, requestId opcional.
- Resultado de diseño: 201 nueva o 200 existente.
- Invariante: Prestador inicia solo tras asignación; comprobar bloques y membresía; consultar chatEnabled actual para nueva conversación; acceso por rol/contexto autorizado.
- Referencia funcional del catálogo: `msgto, msgreq, mchat`; entidad: `conversations`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-266 → T-266 → AC-266.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-067 — GET /me/conversations

- Propietario: S05; actor: Participante; superficie: pública/autenticada según actor.
- Objetivo: Listar bandeja y no leídos por modo.
- Entradas de diseño: mode, unreadOnly, cursor, limit.
- Resultado de diseño: 200 conversaciones propias.
- Invariante: No exponer conversaciones de otro modo/miembro
- Referencia funcional del catálogo: `V.inbox, inf`; entidad: `conversations`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-267 → T-267 → AC-267.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-068 — GET /conversations/{id}/messages

- Propietario: S05; actor: Miembro; superficie: pública/autenticada según actor.
- Objetivo: Recuperar historial o mensajes tras desconexión.
- Entradas de diseño: before/after cursor, limit.
- Resultado de diseño: 200 mensajes y secuencia.
- Invariante: Autorización en cada página; adjuntos privados bajo control
- Referencia funcional del catálogo: `V.chat`; entidad: `messages`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-268 → T-268 → AC-268.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-069 — POST /conversations/{id}/messages

- Propietario: S05; actor: Miembro sin bloqueo; superficie: pública/autenticada según actor.
- Objetivo: Enviar texto o foto aprobados.
- Entradas de diseño: clientMessageId, text o mediaId, requestId/orderId opcionales autorizados.
- Resultado de diseño: 201 mensaje con id/sequence.
- Invariante: Moderar antes de persistir; retry devuelve mismo mensaje; no respuesta simulada; texto 1..600; respuesta rápida y manual comparten SendMessage sin sobrescribir el texto seleccionado.
- Referencia funcional del catálogo: `msgsend, msgquick, chatfile`; entidad: `messages`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-269 → T-269 → AC-269.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-070 — PUT /conversations/{id}/read

- Propietario: S05; actor: Miembro; superficie: pública/autenticada según actor.
- Objetivo: Marcar lectura hasta secuencia.
- Entradas de diseño: lastReadSequence.
- Resultado de diseño: 204.
- Invariante: Monótono; no avanzar más allá de mensaje disponible
- Referencia funcional del catálogo: `chatOpen, unreadAll`; entidad: `conversations`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-270 → T-270 → AC-270.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-071 — PUT /conversations/{id}/block

- Propietario: S05; actor: Miembro; superficie: pública/autenticada según actor.
- Objetivo: Bloquear a contraparte.
- Entradas de diseño: id.
- Resultado de diseño: 204.
- Invariante: Revalidar bloqueo al enviar; no solo silenciar UI
- Referencia funcional del catálogo: `chatblock`; entidad: `blocks`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-271 → T-271 → AC-271.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-072 — DELETE /conversations/{id}/block

- Propietario: S05; actor: Autor del bloqueo; superficie: pública/autenticada según actor.
- Objetivo: Desbloquear contraparte.
- Entradas de diseño: id.
- Resultado de diseño: 204.
- Invariante: No retirar bloqueo impuesto por la otra persona
- Referencia funcional del catálogo: `chatblock`; entidad: `blocks`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-272 → T-272 → AC-272.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-073 — POST /realtime/ticket

- Propietario: S01; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Emitir ticket de conexión WebSocket.
- Entradas de diseño: deviceId.
- Resultado de diseño: 200 ticket de un uso, 30 segundos.
- Invariante: Guardar hash/TTL en Redis de seguridad; no JWT en URL duradera
- Referencia funcional del catálogo: `Requisito tiempo real`; entidad: `sessions`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-273 → T-273 → AC-273.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-074 — WS /ws/v1/chat

- Propietario: S05; actor: Ticket y membresía vigentes; superficie: WS con ticket.
- Objetivo: Recibir avisos de mensajes y pedidos.
- Entradas de diseño: ticket, suscripciones autorizadas, typing/read.
- Resultado de diseño: Eventos versionados con cursor y acuse.
- Invariante: Validar membresía, sesión y bloqueos durante conexión; reanudar por REST
- Referencia funcional del catálogo: `V.chat, navBadge`; entidad: `messages`.
- Spec primaria: [nv-11](../../specs/arquitectura/nv-11/spec.md); FR-274 → T-274 → AC-274.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [011-mensajeria](../../../App_NexoLocal/specs/011-mensajeria/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-075 — POST /media

- Propietario: S06; actor: Cuenta; superficie: pública/autenticada según actor.
- Objetivo: Moderar y registrar imagen.
- Entradas de diseño: multipart bytes, purpose, MIME.
- Resultado de diseño: 201 mediaId aprobado.
- Invariante: Sin almacenamiento persistente previo; máximo 3 MB; validar contenido real
- Referencia funcional del catálogo: `obfiles, pfile, myphoto, chatfile`; entidad: `media`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-275 → T-275 → AC-275.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-076 — GET /media/{id}/access

- Propietario: S06; actor: Público o miembro según recurso; superficie: pública/autenticada según actor.
- Objetivo: Resolver acceso autorizado a medio.
- Entradas de diseño: id.
- Resultado de diseño: 200 URL pública o privada de corta duración.
- Invariante: Revalidar boundResource; no entregar URL privada a visitante
- Referencia funcional del catálogo: `Fotos de perfil/trabajos/chat`; entidad: `media`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-276 → T-276 → AC-276.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-077 — DELETE /media/{id}

- Propietario: S06; actor: Propietario o proceso de eliminación; superficie: pública/autenticada según actor.
- Objetivo: Retirar archivo autorizado.
- Entradas de diseño: id.
- Resultado de diseño: 204 o 409 si vínculo activo requiere editar recurso.
- Invariante: Comprobar referencias y purgar CDN; no borrar medio ajeno
- Referencia funcional del catálogo: `pubimgdel, eliminación`; entidad: `media`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-277 → T-277 → AC-277.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-078 — POST /reports

- Propietario: S06; actor: Cuenta; superficie: pública/autenticada según actor.
- Objetivo: Reportar perfil, trabajo, experiencia o chat.
- Entradas de diseño: targetType, targetId, reasonCode.
- Resultado de diseño: 201 reportId.
- Invariante: Validar acceso al objetivo; no copiar conversación completa a logs
- Referencia funcional del catálogo: `sendreport`; entidad: `reports`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-278 → T-278 → AC-278.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-079 — GET /me/reports

- Propietario: S06; actor: Reportante; superficie: pública/autenticada según actor.
- Objetivo: Consultar estado de sus reportes.
- Entradas de diseño: cursor, limit.
- Resultado de diseño: 200 estado y resolución visible.
- Invariante: No exponer identidad del moderador ni datos de terceros innecesarios
- Referencia funcional del catálogo: `Complemento de seguimiento`; entidad: `reports`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-279 → T-279 → AC-279.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-080 — GET /admin/reports

- Propietario: S06; actor: Moderador; superficie: administrativa.
- Objetivo: Listar cola de revisión.
- Entradas de diseño: status, type, cursor, limit.
- Resultado de diseño: 200 expedientes autorizados.
- Invariante: Acceso administrativo separado; auditoría de consulta sensible
- Referencia funcional del catálogo: `Operación de moderación`; entidad: `reports`.
- Spec primaria: [nv-24](../../specs/arquitectura/nv-24/spec.md); FR-280 → T-280 → AC-280.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-081 — POST /admin/reports/{id}/decision

- Propietario: S06; actor: Moderador; superficie: administrativa.
- Objetivo: Resolver y ordenar ocultación o restauración.
- Entradas de diseño: decision, rationale, If-Match.
- Resultado de diseño: 200 decisión y evento.
- Invariante: No escribir base ajena; propietario aplica evento idempotente
- Referencia funcional del catálogo: `Operación de moderación`; entidad: `reports`.
- Spec primaria: [nv-24](../../specs/arquitectura/nv-24/spec.md); FR-281 → T-281 → AC-281.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-082 — POST /assist/draft

- Propietario: S06; actor: Cuenta limitada; superficie: pública/autenticada según actor.
- Objetivo: Generar borrador editable de solicitud o trabajo.
- Entradas de diseño: kind, categoryId, userText, lengthLimit.
- Resultado de diseño: 200 draft y modelVersion.
- Invariante: No persistir texto por defecto; sin datos precisos; moderar salida
- Referencia funcional del catálogo: `Adición obligatoria IA generativa`; entidad: `reports`.
- Spec primaria: [nv-13](../../specs/arquitectura/nv-13/spec.md); FR-282 → T-282 → AC-282.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [013-inteligencia](../../../App_NexoLocal/specs/013-inteligencia/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-083 — PUT /me/devices/{id}

- Propietario: S07; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Registrar token push y preferencias.
- Entradas de diseño: token, platform, preferences.
- Resultado de diseño: 200 dispositivo.
- Invariante: Asociado a sid actual; token único; rotación
- Referencia funcional del catálogo: `Requisito push`; entidad: `devices`.
- Spec primaria: [nv-21](../../specs/arquitectura/nv-21/spec.md); FR-283 → T-283 → AC-283.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [022-push-dispositivos](../../../App_NexoLocal/specs/022-push-dispositivos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-084 — DELETE /me/devices/{id}

- Propietario: S07; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Desvincular push del dispositivo.
- Entradas de diseño: id.
- Resultado de diseño: 204.
- Invariante: Idempotente; no afectar otro usuario
- Referencia funcional del catálogo: `Logout/push`; entidad: `devices`.
- Spec primaria: [nv-21](../../specs/arquitectura/nv-21/spec.md); FR-284 → T-284 → AC-284.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [022-push-dispositivos](../../../App_NexoLocal/specs/022-push-dispositivos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-085 — GET /me/notifications

- Propietario: S07; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Consultar notificaciones persistentes.
- Entradas de diseño: cursor, limit, unreadOnly.
- Resultado de diseño: 200 lista.
- Invariante: Payload mínimo, acceso al destino se revalida al abrir
- Referencia funcional del catálogo: `notifSheet, bell`; entidad: `notifications`.
- Spec primaria: [nv-21](../../specs/arquitectura/nv-21/spec.md); FR-285 → T-285 → AC-285.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [022-push-dispositivos](../../../App_NexoLocal/specs/022-push-dispositivos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-086 — PUT /me/notifications/{id}/read

- Propietario: S07; actor: Propietario; superficie: pública/autenticada según actor.
- Objetivo: Marcar aviso leído.
- Entradas de diseño: id.
- Resultado de diseño: 204.
- Invariante: Solo aviso propio, idempotente
- Referencia funcional del catálogo: `seenotif`; entidad: `notifications`.
- Spec primaria: [nv-21](../../specs/arquitectura/nv-21/spec.md); FR-286 → T-286 → AC-286.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [022-push-dispositivos](../../../App_NexoLocal/specs/022-push-dispositivos/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-087 — POST /internal/moderation/check

- Propietario: S06; actor: Identidad de servicio autorizada; superficie: interna entre servicios.
- Objetivo: Moderar texto de una operación de negocio.
- Entradas de diseño: content, hash, purpose, userId.
- Resultado de diseño: 200 decisión/proof o 422 rechazo.
- Invariante: API interna, sin persistir contenido rechazado ni log de cuerpo
- Referencia funcional del catálogo: `Requisito 16`; entidad: `reports`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-287 → T-287 → AC-287.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-088 — GET /internal/orders/{id}/review-eligibility

- Propietario: S04; actor: Identidad S03; superficie: interna entre servicios.
- Objetivo: Verificar opinión vinculada a pedido.
- Entradas de diseño: id, actorId.
- Resultado de diseño: 200 elegibilidad y versión.
- Invariante: Devuelve solo pertenencia/done/providerId, nunca domicilio
- Referencia funcional del catálogo: `publishexp con pedido`; entidad: `orders`.
- Spec primaria: [nv-10](../../specs/arquitectura/nv-10/spec.md); FR-288 → T-288 → AC-288.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-089 — GET /health/live

- Propietario: Transversal; actor: Infraestructura interna; superficie: infraestructura.
- Objetivo: Comprobar proceso vivo.
- Entradas de diseño: Ninguna.
- Resultado de diseño: 200 sin detalles sensibles.
- Invariante: No consultar todas las dependencias en liveness
- Referencia funcional del catálogo: `Operación`; entidad: `outbox_inbox`.
- Spec primaria: [nv-01](../../specs/arquitectura/nv-01/spec.md); FR-289 → T-289 → AC-289.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-090 — GET /health/ready

- Propietario: Transversal; actor: Infraestructura interna; superficie: infraestructura.
- Objetivo: Comprobar capacidad de servir tráfico.
- Entradas de diseño: Ninguna.
- Resultado de diseño: 200 o 503.
- Invariante: Comprobar dependencias indispensables del servicio, no datos de usuario
- Referencia funcional del catálogo: `Operación`; entidad: `outbox_inbox`.
- Spec primaria: [nv-01](../../specs/arquitectura/nv-01/spec.md); FR-290 → T-290 → AC-290.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-091 — GET /providers/{id}/contact

- Propietario: S02; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Obtener número del prestador para copiar.
- Entradas de diseño: id.
- Resultado de diseño: 200 teléfono y canales autorizados.
- Invariante: Respuesta privada sin caché compartida; número validado por identidad; no mezclar en DTO público cacheable; exigir showPhone=true vigente; no confiar solo en canal visible en Flutter.
- Referencia funcional del catálogo: `contactSheet, copy`; entidad: `providers`.
- Spec primaria: [nv-25](../../specs/arquitectura/nv-25/spec.md); FR-291 → T-291 → AC-291.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [025-contacto-consentimiento](../../../App_NexoLocal/specs/025-contacto-consentimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-092 — GET /orders/{id}/contact

- Propietario: S04; actor: Participante habilitado; superficie: pública/autenticada según actor.
- Objetivo: Obtener contacto privado de la contraparte.
- Entradas de diseño: id.
- Resultado de diseño: 200 teléfono autorizado.
- Invariante: Cliente: teléfono del prestador solo con showPhone vigente. Prestador: teléfono del cliente solo asignado en ok/done y clientContactPreference=both vigente. Cancelado o revocado: 403/404. No caché de contacto; comprobación autoritativa.
- Referencia funcional del catálogo: `chatmenu, V.match, copy`; entidad: `orders`.
- Spec primaria: [nv-25](../../specs/arquitectura/nv-25/spec.md); FR-292 → T-292 → AC-292.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [025-contacto-consentimiento](../../../App_NexoLocal/specs/025-contacto-consentimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-093 — GET /mentions/suggestions

- Propietario: S02; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Buscar prestador para mencionar en una experiencia.
- Entradas de diseño: q opcional normalizado sin @, lat/lng para vacío, limit 1..5.
- Resultado de diseño: 200 id, nombre, avatar, oficio y zona aproximada.
- Invariante: Excluir perfil propio/inactivo; sin q sugerir cobertura cercana; con q buscar nombre/oficio sin imponer cercanía, como mresHTML. Sin teléfono ni coordenadas privadas. Búsqueda infija debe preservar resultados mediante índice/adaptador medido; no sustituirla silenciosamente por solo prefijo.
- Referencia funcional del catálogo: `mresHTML, mentionHTML, exptag`; entidad: `providers`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-293 → T-293 → AC-293.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-094 — GET /me/reviews

- Propietario: S03; actor: Autor autenticado; superficie: pública/autenticada según actor.
- Objetivo: Listar experiencias propias.
- Entradas de diseño: cursor, limit, status opcional.
- Resultado de diseño: 200 experiencias propias y nextCursor.
- Invariante: Filtrar authorId del JWT; incluir prestador y estado visible al autor; sin depender del nombre Tú.; caso de aceptación obligatorio: Ana publica, logout, Carlos entra y no la obtiene como propia.
- Referencia funcional del catálogo: `V.myprofile, mtab`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-294 → T-294 → AC-294.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-095 — GET /reviews/{id}

- Propietario: S03; actor: Público o autor según estado; superficie: pública/autenticada según actor.
- Objetivo: Abrir experiencia individual y su prestador.
- Entradas de diseño: id.
- Resultado de diseño: 200 DTO público o propio; 404 si no visible.
- Invariante: Complemento de navegación: necesario para enlace estable/notificación propuesta, no pantalla individual confirmada en maqueta.
- Referencia funcional del catálogo: `Complemento de contrato para referencias y enlaces`; entidad: `reviews`.
- Spec primaria: [nv-19](../../specs/arquitectura/nv-19/spec.md); FR-295 → T-295 → AC-295.
- Estado: Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada. formalContract sigue null.
- Consumidor móvil primario: [020-experiencias-reputacion](../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-096 — PATCH /me/provider/contact-preferences

- Propietario: S02; actor: Prestador propietario; superficie: pública/autenticada según actor.
- Objetivo: Configurar chat y visibilidad del número.
- Entradas de diseño: chatEnabled, showPhone, If-Match.
- Resultado de diseño: 200 preferencias y version.
- Invariante: showPhone=false revoca lectura de teléfono inmediatamente. chatEnabled=false impide nuevas conversaciones; propuesta: historial y pedidos activos conservan chat. S05 consulta regla vigente. Alias local wa significa chat interno, no WhatsApp.
- Referencia funcional del catálogo: `V.account, ACT.chan`; entidad: `providers`.
- Spec primaria: [nv-25](../../specs/arquitectura/nv-25/spec.md); FR-296 → T-296 → AC-296.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [025-contacto-consentimiento](../../../App_NexoLocal/specs/025-contacto-consentimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-097 — PATCH /orders/{id}/contact-preference

- Propietario: S04; actor: Cliente propietario del pedido; superficie: pública/autenticada según actor.
- Objetivo: Dar o retirar permiso para compartir su teléfono.
- Entradas de diseño: contactPreference chat/both, If-Match.
- Resultado de diseño: 200 preferencia y version.
- Invariante: Ampliación de privacidad: en pend/ok/done permite revocar; cancel no admite conceder. Cambiar a both no revela teléfono antes de ok. S04 es autoridad y retira cachés de contacto.
- Referencia funcional del catálogo: `CH_OPT y corrección de ACT.chatmenu`; entidad: `orders`.
- Spec primaria: [nv-25](../../specs/arquitectura/nv-25/spec.md); FR-297 → T-297 → AC-297.
- Estado: Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada. formalContract sigue null.
- Consumidor móvil primario: [025-contacto-consentimiento](../../../App_NexoLocal/specs/025-contacto-consentimiento/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-098 — POST /graphql/social/v1

- Propietario: S08; actor: Cuenta autenticada; consultas acotadas; superficie: pública/autenticada según actor.
- Objetivo: Consultar contexto y candidatos relacionados del grafo.
- Entradas de diseño: operationName permitido, variables tipadas, hash de operación registrada; first 1..20.
- Resultado de diseño: 200 data/errors, generatedAt, stale; error explícito o fallback REST disponible.
- Invariante: Extensión opcional de lectura, sin mutations ni Cypher arbitrario. Authorization por recurso/campo, dos saltos y profundidad propuesta 4; máximo 100 objetos, una operación y timeout 2 s. Coste y rate limit por usuario. No caché compartida de datos personalizados.
- Referencia funcional del catálogo: `Propuesta S08; operaciones SocialProviderContext y RelatedProviders`; entidad: `social_graph`.
- Spec primaria: [nv-15](../../specs/arquitectura/nv-15/spec.md); FR-298 → T-298 → AC-298.
- Estado: Opcional; no bloquea alcance obligatorio si no se habilita. formalContract sigue null.
- Consumidor móvil primario: [015-relaciones-opcional](../../../App_NexoLocal/specs/015-relaciones-opcional/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.

## EP-099 — GET /catalog/report-reasons

- Propietario: S06; actor: Cuenta autenticada; superficie: pública/autenticada según actor.
- Objetivo: Obtener motivos y política vigente de reporte.
- Entradas de diseño: locale es-MX, version opcional.
- Resultado de diseño: 200 reasonCodes activos y policyVersion, ETag.
- Invariante: Complemento de contrato: frontend y backend comparten catálogo versionado de motivos; no recopilar datos privados en etiquetas.
- Referencia funcional del catálogo: `Reportar: seis motivos visibles; normalización de catálogo`; entidad: `reports`.
- Spec primaria: [nv-05](../../specs/arquitectura/nv-05/spec.md); FR-299 → T-299 → AC-299.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumidor móvil primario: [005-contenido-seguro](../../../App_NexoLocal/specs/005-contenido-seguro/spec.md). La app enlaza autoridad, no redefine payload.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-100 — GET /internal/providers/{id}/contact-policy

- Propietario: S02; actor: Identidad S04/S05 autorizada; superficie: interna entre servicios.
- Objetivo: Comprobar política actual de contacto del prestador.
- Entradas de diseño: providerId y contexto autenticado de servicio.
- Resultado de diseño: 200 chatEnabled, showPhone, status, policyVersion; sin teléfono.
- Invariante: Solo servicios permitidos; S05 verifica antes de nueva conversación y S04 antes de contacto. No conceder por caché obsoleta; dependencia caída niega divulgación.
- Referencia funcional del catálogo: `Regla autoritativa para EP-066/091/092/096`; entidad: `providers`.
- Spec primaria: [nv-25](../../specs/arquitectura/nv-25/spec.md); FR-300 → T-300 → AC-300.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Leer no cambia estado; comprobar filtros/cursor cuando apliquen.

## EP-101 — POST /internal/accounts/visibility-batch

- Propietario: S01; actor: Identidad S02/S03/S08 autorizada; superficie: interna entre servicios.
- Objetivo: Revalidar visibilidad de autores antes de publicar resultados derivados.
- Entradas de diseño: Hasta 50 accountIds y purpose permitido.
- Resultado de diseño: 200 id, canAppearPublicly y statusVersion; sin PII.
- Invariante: Consulta interna de lectura, no mutation. Si hay borrado/suspensión, excluir autor y recursos según política del propietario; no devolver teléfono, secreto o ubicación. No log del cuerpo.
- Referencia funcional del catálogo: `Revalidación del grafo y prevención de mezcla entre cuentas`; entidad: `accounts`.
- Spec primaria: [nv-04](../../specs/arquitectura/nv-04/spec.md); FR-301 → T-301 → AC-301.
- Estado: Previsto en línea base; contrato formal y aceptación pendientes. formalContract sigue null.
- Consumo móvil directo: ninguno. La app solo prueba el efecto de negocio por su API pública; no invoca esta superficie.
- Ensayo: permitido obtiene resultado previsto; entradas inválidas, estado/identidad no permitidos o recurso retirado no filtran datos; fallos/reintentos siguen contrato. Confirmar efecto durable/idempotencia o ticket/secuencia según operación; nunca interpretar timeout como éxito.
