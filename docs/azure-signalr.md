# Azure SignalR para Nexo Local

## Configuracion
Crear un recurso Azure SignalR en modo Default. Configurar Azure__SignalR__ConnectionString solo en el entorno de la API (o .env global ignorado). Usar recursos distintos por entorno; las replicas de un entorno comparten recurso. No copiar claves a Flutter. Guardar el .env antes de reiniciar el Host. En produccion usar HTTPS y gestionar el secreto mediante Key Vault o adoptar identidad administrada.

Si la cadena esta vacia, se conserva SignalR directo con backplane Redis. Si esta presente se usa Microsoft.Azure.SignalR 1.33.1 y no se registra backplane Redis para SignalR. Redis sigue siendo obligatorio para tickets, sesiones, limites y otras funciones.

## Seguridad y compatibilidad
POST autenticado /v1/engagement/hubs/chat/ticket devuelve ticket, expiresInSeconds, url y negotiate. Azure: negotiate=true, Flutter hace POST /hubs/chat/negotiate con ticket. Middleware consume GETDEL, rechaza tickets invalidos/reutilizados y establece nexo_chat_account desde Redis, nunca desde identidad suministrada. SDK Azure transporta solo esa claim al hub; el grupo se deriva de esa identidad. El JWT de sesion no se coloca en la URL. El SDK utiliza su propio token de conexion de corta vida; no registrar URLs ni respuesta de negotiate. Ticket sigue caducando a los 60 segundos. Un ticket nuevo se solicita al reconectar.

Sin Azure: negotiate=false y WebSocket directo conserva el consumo original del ticket. Clientes antiguos ignoran negotiate y no son compatibles con Azure; desplegar app actualizada junto con la activacion del servicio. No hay migracion de historial. E2EE, estados y adjuntos permanecen en sus rutas existentes; Azure transporta avisos. Un aviso no prueba entrega al dispositivo.

## Verificacion operativa pendiente
Reiniciar API tras guardar configuracion. Usar dos cuentas/dispositivos, comprobar POST negotiate y redireccion a Azure sin registrar token, dos conexiones cliente y conexiones servidor en metricas. Probar mensaje entre replicas, edicion/eliminacion, lectura/entrega, reconexion con ticket nuevo y recuperacion HTTP. Simular perdida de red y servicio; nunca perder persistencia ni convertir aviso fallido en envio fallido. Verificar aislamiento entre cuentas y cifrado de adjuntos. Medir carga antes de dimensionar.

Rollback: retirar la variable Azure y reiniciar todas las replicas; la app nueva usa negotiate=false. Mantener Redis disponible. No mezclar replicas con transportes diferentes. No se declara validacion cloud o productiva por compilar o por pruebas con dobles.

Evidencia 2026-11-21: build API completo Release 0 errores/0 advertencias; ChatNegotiationTests 6/6 con dobles; integridad SDD local aprobada. Prueba cloud aislada con SDK .NET, ChatHub real y Redis local: negociacion Azure, entrega a grupo privado y rechazo de ticket reutilizado 401 aprobados. Cliente Flutter ChatLive real: 1/1 prueba cloud recibiendo evento al grupo privado. Contratos HTTP Flutter 10/10 con dobles; analyze sin incidencias. No se acredita prueba de carga, despliegue ni recorrido en dispositivos. Se inicio Redis local para esta validacion; no se reinicio la API del usuario.
