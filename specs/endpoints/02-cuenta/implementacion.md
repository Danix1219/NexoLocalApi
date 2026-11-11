# Incremento de cuenta — decisión ejecutable 2026-11-09

Siguiente conjunto funcional de Identity después de 01-auth. No confundir el número de carpeta
con nv-02, que gobierna contratos; la cobertura funcional se incorpora en nv-04. La confirmación
SMS está retirada: son ocho operaciones vigentes. El encargo del usuario autoriza su construcción;
no sustituye revisión humana ni aceptación global de nv-12 (borrado distribuido).

## Orden y límites

1. GET propio con proyección sin hash; sin caché interna para evitar datos obsoletos entre réplicas.
2. Modo, móvil declarado y dispositivos, con estado/pertenencia comprobados en persistencia.
3. PATCH: distingue ausente/null; modera nombre antes de guardar y resuelve subida aprobada propia.
4. Baja y cancelación: cambios condicionales, evento durable y recuperación restringida.

El host conserva sus cinco capas. No se crean proyectos ni referencias a otros dominios.
Cambios personales y eventos se guardan atómicamente en el mismo documento; un relay idempotente
los lleva al outbox. Un fallo del outbox no pierde la intención del cambio confirmado.
La escritura usa revisión optimista; un conflicto se responde 409 y se puede reintentar.

GET no usa L1: prima la lectura actual por _id, sin invalidación distribuida ficticia.
PATCH rechaza propiedades desconocidas y null en name; null explícito en photoUploadId borra foto.
Una subida no aprobada o ajena responde el mismo 404 en una consulta aprobada+propietario.
Moderación real usa Azure Content Safety REST 2024-09-01; configuración ausente/fallo devuelve 503,
nunca aprobación simulada. Su integración cloud requiere credenciales y aceptación independiente.

La baja devuelve 202 con plazo de 30 días. Las réplicas consultan estado vigente para negar acciones
en baja, incluso con JWT anterior. Login/refresh durante la gracia emiten únicamente en /connect/token
un token con recovery_only=true; solo permite consultar cuenta, cancelar baja, repetir la solicitud
pendiente con contraseña o cerrar sesión. Los JWT y refresh de épocas anteriores son rechazados,
incluso si una revocación secundaria del servidor de tokens hubiera fallado.
Tras cancelar, renovar para salir de ese acceso restringido. Fuera de plazo, cancelar devuelve 410.
No se devuelven conteos inventados de otros dominios: restorationStatus=queued expresa propagación.
Los eventos no acreditan que Discovery/Engagement hayan restaurado o eliminado datos.

Las dependencias globales (pedidos, medios externos y anonimización distribuida de nv-12) conservan
su dueño. No leer bases ajenas para aparentar validación de pedidos o restauración completada.
Antes de aceptar globalmente la baja se exige la integración de esos consumidores y su job.

## Preparación y comprobaciones

Rutas: Account.cs, Application/Features/AccountManagement.cs, Application/Ports/AccountPorts.cs,
Infrastructure/Persistence/AccountManagementStore.cs, Infrastructure/Messaging/AccountEventRelay.cs,
Infrastructure/Safety/AccountProfileSafety.cs, Endpoints/Http/AccountEndpoints.cs y TokenEndpoint.cs.
Registro en DI existente; seguridad de estados en IdentityAccountAccess.cs. Pruebas de cuenta en
Nexo.Seguridad.Tests, con Mongo/Redis reales y dobles explícitos para moderación cloud.

Comandos: build Release, format verify-no-changes, suite sin carga de .env, wrapper SDD en workspace
aislado e integridad. Probar casos válidos, 400/401, BOLA, estados, repetición, concurrencia y eventos.
Sin credenciales reales ni correos a personas; no publicar ni desplegar. Revertir el incremento
no borra datos; no retirar la protección de tokens restringidos mientras sigan vigentes.

## Evidencia local de cierre (2026-11-09)

Build Release y format correctos con SDK 10.0.302. Suite final: 96 pruebas correctas, de ellas
40 nuevas de cuenta; cero fallidas/omitidas. Mongo y Redis reales; moderación cloud y aviso
de correo con dobles explícitos. SDD: 26 specs, traza e integridad documental correctas y
8 pruebas del validador. No se verificaron Gmail ni Content Safety reales ni anonimización global.
El `.env` personal no contiene configuración de Content Safety: PATCH de nombre responde 503
hasta configurarlo. No se omite esa protección para dar una aceptación ficticia.
