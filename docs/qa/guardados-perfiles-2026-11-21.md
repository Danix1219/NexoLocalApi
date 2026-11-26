# Guardados y perfiles compartidos - 2026-11-21

Causa: el payload guardado omitio el resumen de prestador; Flutter parseaba un objeto vacio y mostraba nombre vacio/avatar ?. Se agrega una consulta por lote de perfiles publicados, sin contacto privado. Solicitudes se enriquecen con IRequestClients; autores inactivos no aparecen. Los guardados conservan saved=true y recorren todos los cursores. Perfil solicitante comparte un identificador aleatorio activado solo mediante accion explicita; lectura publica minima y revocacion soportada.

Validacion en curso: compilacion Host Release aprobada; 66 pruebas Flutter aprobadas; dos pruebas Mongo aprobadas antes del ajuste final de solicitudes. Registrar ejecucion final antes del cierre. Necesita reiniciar API local para cargar nuevos endpoints; no se ha reiniciado el proceso del usuario.

Resultado final: tres pruebas de integracion con MongoDB/Redis aprobadas. Flutter: 66 pruebas aprobadas y analisis de nueve archivos sin incidencias. Compilacion APK en curso. Pendientes: reinicio API, aceptacion visual humana y seguridad HTTP completa.

APK debug live compilado e instalado mediante adb install -r en Xiaomi 096fd2cc784f y Motorola ZY32JKH7KW: Success en ambos, actividad iniciada. No se borraron datos ni se reinicio la API. Reinicio API y aceptacion visual humana pendientes.

Validadores SDD de API y app ejecutados: FAIL por referencias historicas rotas y restricciones de fase documental; no se declara aprobacion SDD global.
