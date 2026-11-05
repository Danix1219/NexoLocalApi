# G01 — Gateway de entrada

Diseño de implementacion propuesto, 2026-11-03. Proyecto original existente; host vacio sin proxy instalado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `nexolocal.Api/`. Sin base ni agregados de negocio. Configuracion de rutas y limites; no copiar cuentas, mensajes o pedidos. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-01](../../../specs/arquitectura/nv-01/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Proyecto original `nexolocal.Api.csproj`, sin referencias a proyectos de negocio. Carpetas siguientes son responsabilidades futuras dentro del host, no nuevos microservicios ni proyectos creados:

```text
Routing/
Composition/
Security/
Resilience/
Observability/
```

G01 conserva un único host organizado por enrutamiento, composición, seguridad, resiliencia y observabilidad. No requiere Application/Domain de negocio ni adaptadores de persistencia. Las reglas por recurso permanecen también en el servicio dueño.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Gateway/BFF acotado | Entrada HTTPS/WS y composicion autorizada de respuestas; cada backend conserva su regla y autorizacion por recurso. | Configurar por contrato de servicio, no por cada boton de Flutter. |
| Timeout + aislamiento por dependencia | Presupuesto de tiempo por destino y concurrencia acotada; evitar cascada si moderacion/chat fallan. | Circuit breaker si se justifica; no reintentar POST mutantes por omision. |
| Rate limiting y cache publica segura | Limitar abuso y cachear solo representaciones publicas con version/TTL e invalidacion. | No cachear permisos positivos, tokens, telefono o domicilio exacto. |

## Persistencia y consistencia

No coordina transacciones ni saga de negocio. No referencias de proyecto hacia S01-S08, ni acceso directo a bases ajenas. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Clientes HTTP/WS por destino y compositores de lectura autorizados; ningun repositorio de negocio. YARP es propuesta pendiente, no implementacion actual.

Flujo: Peticion → identidad/limites de entrada → destino propietario → resultado/timeout explicito. WS se enruta hacia S05; la conexion no sustituye su verificacion de participante.

## Operaciones ya asignadas (sin inventar endpoints)

G01 no es dueño primario de operaciones de negocio en el catalogo; enruta las del dueño. EP-089/EP-090 son transversales y no se reasignan exclusivamente al gateway. No inventar un endpoint para justificar cada patron.

## Criterios antes de implementar/aceptar

- Destino caido no bloquea todos los servicios; timeout acotado y error consistente.
- G01 no referencia DLL/proyectos de negocio ni filtra datos privados en una composicion.
- Cache/invalidation y limites funcionan entre replicas; no devolver una respuesta privada cacheada.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.
