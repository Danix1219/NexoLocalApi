# Incremento ejecutable 05-trabajos — 2026-11-10

## Spec y decisiones
Dueño Discovery / nv-08; cinco fichas vigentes gobiernan HTTP: POST works, GET providers/{id}/works, DELETE works/{id}, POST/DELETE works/{id}/recommend. El encargo autoriza desarrollo local y pruebas, no publicación, cambios móviles ni aprobación humana global.

Refinamiento de entradas antes de construir:
- La línea histórica nv-08 usa 4 imágenes/texto 200/título automático; las fichas vigentes definen título 5..90, texto 20..1000, hasta 6 imágenes y campos opcionales problem/did/time/tag. Se mantiene el contrato de las fichas por constitución §5, sin añadir edición/detalle que no tienen ficha vigente.
- Moderar TODOS los textos antes de persistir; reutilizar IProviderSafety con Content Safety real y fallo cerrado. No inventar aprobación si falta configuración. Categoría contra perfil publicado y cuenta activa por contrato Identity, nunca contra modo de UI.
- Las imágenes requieren ciclo real de subida/moderación/purga del grupo 14. Con configuración actual no disponible, validar IDs propios/aprobados y responder 503, sin publicar URLs cuya retirada física no pueda garantizarse. Trabajos sin imágenes sí tienen lógica operativa, condicionada a Content Safety. Borrado de un documento legacy con imágenes devuelve 503 conservando contenido hasta habilitar su purga; no simular 204.
- No persistir coordenadas exactas en works, idempotencia, eventos o logs. Difuminar una sola vez 500–800 m y persistir únicamente publicPoint/zoneLabel; etiqueta de zona aproximada, no domicilio. No enviar punto exacto a geocodificación para este caso.
- Transacción local Mongo en Discovery para trabajo, contador jobs/recs, recomendación única, cuota diaria e información idempotente/evento durable. Replica set necesario (Atlas lo soporta); pruebas existentes pasan a Mongo Testcontainers replica set real, sin proyecto nuevo.
- Cuota durable de 30 publicaciones por cuenta/día UTC dentro de la misma transacción; se refina el contador Redis histórico para evitar consumo doble o pérdida ante rollback/caída de Redis. Rate limit de abuso por HTTP/Redis existente permanece activo. Una repetición idempotente no consume cuota; fallo de moderación no escribe ni consume.
- Idempotencia duradera por owner+key, hash normalizado y workId; no guarda payload exacto. Reuso con otro contenido 409. Borrado conserva solo recibo mínimo, no texto ni imágenes; reintento del alta ya borrada 404, nunca resucita contenido.
- POST/DELETE recommend son estados deseados idempotentes, sin autobombo, sin revelar quién recomendó. Transacción incrementa/decrementa trabajo y prestador solo ante relación realmente insertada/borrada. Borrado de trabajo elimina recomendaciones/marcados y ajusta ambos contadores sin valores negativos.
- Listado keyset por UUID v7 canónico (cadena), cursor compacto ligado a prestador <=64, límite 1..50 y total desde contador jobs. Sin caché compartida: private,no-store para evitar marcas personales o bajas obsoletas. Visitante no recibe saved/recommended; usuario recibe marcas consultadas en Mongo.
- Eventos de Discovery durables y atómicos con el cambio. Despacho cloud, push, invalidación distribuida/purga y feed quedan pendientes de sus consumidores; no declararlos entregados por existir evento.

## Plan
Modelos de trabajo sin punto exacto en Domain. Application/Works: reglas, puertos, casos de uso, cursor y DTO. Infrastructure/Works: MongoWorkStore transaccional sobre base keyed Discovery; no cliente/base ajena. Endpoints/WorkEndpoints con JWT/rate limit actual y errores estables. Registrar en Discovery existente; sin paquetes ni proyectos nuevos.

## Tareas y criterios
- FR-991: publicar/listar/borrar trabajos propios moderados con categoría válida, privacidad y reintento seguro.
- FR-992: recomendar/quitar recomendación sin duplicar ni revelar identidad, con contadores coherentes.
- NFR-991: atomicidad local, aislamiento, no punto exacto persistido, cuota 30/día y errores de dependencias.
- AC-991: WorkRulesTests/WorkEndpointsTests cubren límites, moderación rechazada/no disponible, autenticación, listado y propiedad.
- AC-992: WorkPersistenceTests/WorkEndpointsTests cubren reintentos/concurrencia, contadores/cuota y rollback con Mongo replica set real.
- AC-993: privacidad en colecciones/HTTP/eventos, marcas por cuenta, cursores ajenos y baja/despublicación excluidas.
- T-991 → FR-991/FR-992/NFR-991: preparar e implementar el subset, validar AC-991/992/993 en Discovery.Unit, Integration, Seguridad.Tests y arquitectura existentes, conciliar evidencia/Memoria. Revisión humana pendiente.

## Verificación prevista
Build Release; suites actuales con cobertura; scripts/check_sdd.ps1 y check_sdd_integrity.py. Docker real para transacciones y HTTP/auth; doble solamente para la dependencia externa de moderación. No atribuir al doble aceptación Azure.

## Evidencia
Resultado final por capas: 197 casos únicos aprobados, 27 nuevos sobre los 170 previos. Architecture.Tests 7; Common.Unit 16; Discovery.Unit 47 (13 nuevos); Identity.Unit 4; Integration 36 (6 nuevos); Seguridad.Tests 87 (8 nuevos). Engagement.Unit conserva cero pruebas y no se cuenta como cobertura.

Ejecución: `dotnet test src/Nexo.sln -c Release --no-restore -m:1 --collect:"XPlat Code Coverage" --logger "trx;LogFileName=works-verified.trx"`. La solución aprobó inicialmente 196 casos. La revisión final corrigió que un trabajo retirado por moderación también pueda ser borrado por su autor; regresión posterior de los 6 WorkPersistenceTests con cobertura, TRX `works-delete-regression.trx`, agrega el caso restante. No se presenta como una única ejecución de 197. Logs en C:/Users/kevin/nexolocal_api/artifacts/works-verified.txt y works-delete-regression.txt; cobertura/TRX en TestResults de las capas existentes.

AC-991: JWT/propiedad/categoría, límites, moderación de TODOS los campos y cierre por dependencia fallida. AC-992: carreras reales de alta idempotente y recomendar/quitar, cuota durable de 30/día y rollback completo si falla el evento. AC-993: solo punto difuminado, recibos sin payload, marcas por cuenta, cursores ligados al prestador y cuenta en baja invisible. La recomendación no expone autores. HTTP/Mongo/Redis son reales; Content Safety es doble explícito y no acredita Azure.

Integridad SDD: 26 specs, 488 tareas, 478 requisitos, 331 criterios. check_sdd.ps1 mantiene enlaces históricos rotos a docs_nexo/App_NexoLocal no presentes: no es SDD-OK global. Revisión humana, Blob/CDN/purga/push/dispatcher y entrega externa pendientes.

Fuentes técnicas: [transacciones C# MongoDB](https://www.mongodb.com/docs/drivers/csharp/current/crud/transactions/) y [replica set en Testcontainers](https://dotnet.testcontainers.org/modules/mongodb/). Los cambios de una transacción se ejecutan secuencialmente con la misma sesión.

## Cierre operativo local
Build Release de src/Nexo.sln: cero errores/advertencias; diff-check 0. API reiniciada en http://127.0.0.1:5188; readiness 200 y cinco operaciones de trabajos presentes en OpenAPI. Sin sesión: POST/DELETE/recommend responden 401; GET de trabajos de un prestador inexistente responde 404, como contrato. Son comprobaciones de ruta/autorización con configuración existente, sin crear una cuenta/trabajo real ni afirmar envío/purga/moderación cloud. Suite HTTP autenticada verificada en Testcontainers.


Actualizaci?n 2026-11-10: el incremento de [medios reales](../14-imagenes-cdn/implementacion.md) sustituye el bloqueo hist?rico de im?genes de este documento. Se admiten ?nicamente uploads propios aprobados para el prop?sito correspondiente; sin CDN ni chat privado.
# Incremento 2026-11-10: edición de trabajo

Se añade [PATCH de trabajo propio](patch-works-id.md) con nueva moderación de texto y validación de hasta seis imágenes. El listado entrega texto completo y mantiene `textExcerpt`. La edición conserva categoría, ubicación pública y recomendaciones.

