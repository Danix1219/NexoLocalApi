# Runbook de entrega — pendiente de implementación

El formato toma como referencia el release por slice del profesor; Nexo sí deberá completar CI/CD, nube y publicación móvil según Móvil v2. No existen todavía recursos ni comandos de despliegue válidos.

1. Identificar spec, versiones, commit y artefactos reales.
2. Confirmar contratos compatibles, migraciones, secretos e infraestructura autorizada.
3. Ejecutar y conservar gates de la fase; no copiar resultados del ejemplo.
4. Entregar a entorno de prueba y verificar recorrido, permisos, dependencias y observabilidad.
5. Confirmar dos instancias HTTP, revocación distribuida, CDN y costos donde corresponde.
6. Verificar AAB/Play, enlaces de landing, privacidad/eliminación, GA4 y QR cuando estén en alcance.
7. Registrar rollback probado, resultado en Memoria, checklist y revisor antes del tag/release.

## Preparación y cierre global

Preparar versiones, compatibilidad, CI y canal Play desde fundamentos, sin esperar la última fase. El cierre de entrega requiere cada fase obligatoria aceptada y trazabilidad de API/app; S08/shortlist opcionales no bloquean si no se habilitan. El [registro de versiones](../../docs_nexo/SDD_VERSIONES.md) precede al primer desarrollo; no asigna versiones ya publicadas.

El paquete académico incluye .rar sin secretos, scripts NoSQL reales, guía reproducible, versiones/manifiesto, entrevistas y evidencia de instalación/evaluación. Cuenta GitHub única, accesos y carpeta compartida siguen pendientes de responsables reales. No publicar ni desplegar por existir este runbook.

## Restaurar un respaldo sin resucitar cuentas eliminadas (nv-12, AC-100)

Estado: **propuesta sin ensayar**. No hay respaldos configurados en este repositorio ni un periodo de retención ratificado.

Un respaldo anterior a una baja devuelve la cuenta y sus datos. Antes de volver a atender tráfico hay que aplicar otra vez las supresiones.

1. Detener el tráfico y arrancar la API con `Accounts__Deletion__DisableWorker=true`.
2. **Antes de restaurar**, exportar de la base actual de Identity (`accounts`) el `_id`, `status` y `deletionRequestedAt` de las cuentas con `status` `deleted` o `pendingDeletion`. No incluye datos de contacto. Si la base actual se perdió, usar la última copia de esta lista guardada fuera de los respaldos; dónde y cada cuánto guardarla está **por decidir**.
3. Restaurar las tres bases al mismo punto en el tiempo.
4. En la base restaurada, para cada cuenta de la lista que exista: `status: "pendingDeletion"`, el `deletionRequestedAt` exportado, `erasedDomains: []`, `suspensionDone: false`, `lifecycleLeaseUntil: null` e incrementar `authEpoch`.
5. Quitar `Accounts__Deletion__DisableWorker` sin abrir todavía el tráfico. El ciclo oculta las pendientes y borra las vencidas. Las ya borradas vuelven a recibir el correo final.
6. Comprobar que todas las que estaban en `deleted` vuelven a estarlo y revisar los avisos de bajas pendientes en los registros.
7. Abrir el tráfico y anotar fecha, punto restaurado y resultado.
