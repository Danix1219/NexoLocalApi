# Chat privado y experiencia de mensajeria

Estado: implementacion local en validacion; no activado en produccion. Fecha: 2026-11-16.
El usuario aprobo priorizar E2EE, sustituir la inspeccion del servidor en chats privados y utilizar un SDK Flutter separado.

## Alcance implementado

- Flutter 3.38.10 / Dart 3.10.9 separado del SDK anterior; CI alineado y libsignal 9.0.1 fijado.
- Identidad Signal por cuenta y dispositivo, claves privadas y sesiones en una boveda SQLite cifrada con AES-256-GCM-SIV; clave maestra en almacenamiento seguro separado del token de acceso. Marcador Android en no_backup para detectar restauraciones incompatibles. Transaccion atomica del ratchet antes de exponer resultados.
- Registro autenticado de claves publicas, Kyber y preclaves. Mongo consume cada preclave en una operacion atomica; repetir registro no repone claves consumidas ni sustituye identidades revocadas.
- Activacion explicita por conversacion con ambos participantes preparados. Tras activarla, el servidor rechaza texto/fotos del contrato legado, incluso ante una carrera con la activacion. El historial antiguo no se cifra retroactivamente.
- Sobre por dispositivo, vinculado a conversacion, emisor, destinatario e identificador de operacion. Reintentos reutilizan ciphertext e identificador persistidos; opcion de reenvio pendiente. Nunca se usa la maqueta como alternativa ante un fallo.
- Fotos y WAV privados: clave y nonce distintos por archivo, cifrado autenticado con conversacion e identificador como datos asociados; claves y tipo dentro del mensaje Signal. Descarga mediante API autenticada; sin SAS ni CDN publico. La app descifra en memoria y no crea un archivo de audio descifrado. El usuario abre expresamente el contenido recibido.
- Voz Android: permiso de microfono solicitado al grabar, hasta 60 segundos, detener, escuchar, cancelar y enviar; captura PCM y WAV en memoria. Reproduccion nativa con MediaDataSource y limpieza al detener o salir.
- Acuse real del receptor mediante POST /v1/engagement/conversations/{id}/delivered; doble marca de entrega y estilo distinto de lectura. Un evento SignalR por si solo no confirma entrega. Lectura se solicita cuando el chat esta visible y se siguen los mensajes recientes.
- Comparacion de numeros de seguridad por par de dispositivos. Los cambios de identidad fijada no se aceptan automaticamente.

## Rutas nuevas (deshabilitadas por defecto)

Grupo /v1/engagement/chat-keys, con autenticacion, limite de peticiones y Cache-Control private, no-store:

- PUT /devices; GET /devices; DELETE /devices/{deviceId}.
- POST /devices/{deviceId}/claim para otro dispositivo propio.
- GET /conversations/{id}/devices y POST /conversations/{id}/devices/{deviceId}/claim.
- POST /conversations/{id}/enable y POST /conversations/{id}/messages.
- PUT y GET /conversations/{id}/attachments/{attachmentId}.

Los adjuntos son ciphertext opaco, hasta 8 MiB en API; la app limita el original a 5 MiB y WAV a 60 segundos. Solo participantes de una conversacion privada no bloqueada pueden cargar y descargar.

## Configuracion para pruebas, aun no para produccion

Crear un contenedor Blob llamado chat-private con acceso publico deshabilitado. No asociarlo a la ruta publica de Front Door/CDN. Configurar Chat__StorageConnectionString con la conexion real de esa cuenta (preferiblemente referencia de Key Vault), sin pegar su valor en documentacion. El adaptador rechaza un contenedor publico y no lo crea automaticamente.

Chat__E2eeEnabled=true habilita las rutas en el backend de pruebas. La app se compila con --dart-define=CHAT_E2EE=true. Ambos usuarios usan Android y eligen Preparar chat privado; la primera preparacion puede no activar hasta que el contacto tambien registre sus claves. La proteccion predeterminada permanece desactivada hasta completar la validacion descrita abajo.

No se necesita que Azure Key Vault reciba claves privadas de chat. E2EE es independiente de la clave de direcciones de pedidos. SignalR/Redis siguen siendo transporte de avisos, no prueba de entrega ni autoridad criptografica.

## Moderacion, reportes y compatibilidad

El chat privado no manda texto, fotos ni voz descifrados a Content Safety. Mantiene bloqueo, limite de abuso y reporte de la conversacion. La pantalla de envio explica la ausencia de moderacion automatica. No hay un clasificador local que garantice contenido adecuado. El envio voluntario de evidencia seleccionada de mensajes al moderador todavia no esta implementado; el reporte actual no adjunta contenido descifrado ni claves.

Android es la plataforma implementada para boveda y audio. Otras plataformas fallan de forma explicita y no degradan a texto en claro. Dependencia libsignal: wrapper de terceros con licencia AGPL-3.0; revisar sus obligaciones antes de distribuir. No se afirma auditoria criptografica independiente ni equivalencia completa con WhatsApp.

## Validacion y pendientes de activacion

Evidencia local final: 46 pruebas de Engagement aprobadas; suite completa Flutter de 279 pruebas aprobada y 6 pruebas HTTP de mensajes aprobadas tras agregar dos casos de entrega y rechazo seguro. Analisis Flutter sin incidencias; 139 archivos comprobados por dart format sin cambios; ANDROID-OK y PIPELINE-OK (54 checks). APK debug compilado con CHAT_E2EE=true. Dos pruebas con Mongo real y replica set temporal aprobadas: consumo concurrente sin reutilizacion, revocacion, rechazo de degradacion, destinatarios ajenos, idempotencia y acuse de entrega. Prueba nativa Signal cubre alteracion, identidad fijada, restauracion de registros, mensajes fuera de orden y autenticacion de adjuntos con contexto distinto. No equivale a dos dispositivos reales ni a Blob de Azure validado.

Pendiente: pruebas con Blob real, dos Android y cuentas distintas, segundo dispositivo, revocacion, perdida/restauracion de claves, agotamiento y reposicion de preclaves, rotacion de claves firmadas, gestion de slots revocados y retencion/borrado de adjuntos huerfanos. Actualmente hay hasta cinco slots inmutables por cuenta y cien preclaves iniciales; no activar ampliamente hasta completar ese ciclo de vida. Verificar logs, outbox y push sin contenido privado; no declarar validacion de producto a partir del lint o de dobles HTTP.

No se encontraron dispositivos ADB conectados durante esta ejecucion. No se modifico Azure ni se publicaron cambios remotos.

## Referencias

- https://github.com/signalapp/libsignal
- https://signal.org/docs/specifications/doubleratchet/
- https://signal.org/docs/specifications/sesame/
- https://pub.dev/packages/libsignal/versions/9.0.1
- https://pub.dev/packages/record/versions/6.2.1

## Preparacion local 2026-11-17

MongoDB replica set, Redis y Azurite iniciados con docker compose; servicios saludables. Contenedor Blob chat-private verificado sin acceso publico: escritura/lectura de bytes aleatorios correcta y acceso anonimo HTTP 403. Variables Chat__E2eeEnabled=true y Chat__StorageConnectionString=UseDevelopmentStorage=true configuradas exclusivamente en .env local; no representan valores de produccion.

API iniciada por el usuario: /health/live y /health/ready HTTP 200 en 127.0.0.1:5188. APK debug compilado con API_MODE=live, CHAT_E2EE=true y API_BASE_URL=http://127.0.0.1:5188. Telefono USB detectado e instalado sin borrar datos. Emulador Medium_Phone_API_35 arrancado y autorizado por el usuario; ambos conectados mediante adb reverse tcp:5188 tcp:5188.

Validacion funcional entre cuentas pendiente de inicio de sesion y ejecucion: texto bidireccional, entrega frente a lectura, imagen privada, voz (cancelar/revisar/enviar), reconexion, bloqueo, identidad y no degradacion. Preparacion de contenedores y health checks no constituyen aceptacion para produccion. Continuan las limitaciones de ciclo de vida de claves y adjuntos documentadas arriba.

## Correccion UX de voz y estados (2026-11-17)

El microfono se ofrece tambien antes de la activacion y conduce a preparar el dispositivo y el chat privado; no inicia grabacion ni envia audio sin cifrar. Enviado muestra una marca gris, Entregado dos marcas grises, Leido dos marcas celestes, con texto visible y semantica accesible. El receptor confirma entrega desde la bandeja tras recuperar mensajes, sin marcar lectura; el chat conserva la condicion de visibilidad para lectura. Refresco de respaldo cada cuatro segundos y recarga pendiente si llega un aviso durante una consulta. Un telefono desconectado o app sin recibir no se declara entregado.

Pruebas locales de contratos HTTP y widgets: 41 aprobadas, incluida diferenciacion de texto, icono y color de cada estado. Instalacion actualizada en dispositivos y recorrido humano pendientes de resultado; no se certifica produccion por estas pruebas.

### Push de mensajes (2026-11-17)
El emisor FCM usa data-only de prioridad alta para nexolocal://chat: deepLink, accountId y type chat.message. No incluye nombre, foto ni texto del mensaje; la app consulta con autenticacion y descifra localmente antes de presentar MessagingStyle. Los demas avisos conservan Notification. Requiere API/worker reiniciado; no se acredita entrega FCM real ni despliegue productivo con la compilacion local.


Validacion del incremento: Nexo.Engagement.Infrastructure Release compilo con 0 errores/0 advertencias usando salida separada artifacts/push-validation. API local /health/live respondio 200; no demuestra que el proceso haya recargado el nuevo emisor ni entrega FCM real.

