# Specs de arquitectura — mapa de consolidación

Versión: 1.0 | Fecha: 2026-11-05 | Estado: propuesta, pendiente de revisión de Johann

Consolidación de **26 specs a 14**, sin perder una sola operación. Entrada: constitución [§3, §4 y enmienda E-001](../../.specify/memory/constitution.md).

## Por qué

El inventario de operaciones arrojó **101 EP únicos**. Nueve specs resultaron ser **subconjuntos exactos** de otros: no aportan ninguna operación propia, pero obligan a mantener dos veces la misma regla, los mismos contratos y los mismos gates.

Los 12 specs primarios suman exactamente los 101 EP. La cobertura está completa sin los duplicados.

## Specs activos (14)

| Spec | Nombre | EP | Absorbe |
|---|---|---|---|
| **nv-00** | Arquitectura base: API modular por dominios | 2 | nv-01 |
| **nv-02** | Contratos, versionado y compatibilidad | — | — |
| **nv-03** | Gates de calidad y DevSecOps | — | — |
| **nv-04** | Identidad, sesión y aislamiento de cuenta | 13 | **nv-16** |
| **nv-05** | Moderación, medios y reportes | 9 | **nv-24** |
| **nv-06** | Perfiles, catálogo, cobertura y edición | 13 | **nv-17**, parte de **nv-25** |
| **nv-07** | Búsqueda, mapa, comparación y feed | 6 | — |
| **nv-08** | Trabajos, experiencias, menciones y guardados | 19 | **nv-18**, **nv-19** |
| **nv-09** | Solicitudes, intereses y candidatos | 11 | **nv-20** |
| **nv-10** | Pedidos, privacidad de contacto y resolución | 12 | parte de **nv-25** |
| **nv-11** | Conversaciones, mensajes y avisos en vivo | 8 | — |
| **nv-12** | Notificaciones y eliminación distribuida | 6 | **nv-21** |
| **nv-13** | ML y asistencia generativa | 1 | — |
| **nv-14** | Infraestructura, operación, CI-CD y landing | — | **nv-22**, **nv-23** |

**Total: 101 EP.** Cobertura completa.

> La numeración **no se renumera** a propósito: `nv-04` se cita desde `docs/` y desde otros specs, y renumerar rompería esas referencias sin ganar nada. Quedan huecos (nv-01, nv-15…) y eso es correcto: el hueco documenta que ahí hubo algo y dónde está ahora.

## Specs consolidados (11)

Cada uno conserva un archivo de redirección que dice dónde quedó su contenido. **Nada se borra sin dejar rastro.**

| Spec | Sus EP | Destino | Motivo |
|---|---|---|---|
| nv-01 Fundación de servicios | EP-089, 090 | **nv-00** | Mismas sondas de salud. El fundamento (outbox, eventos, configuración) es ahora FR-003 y FR-006 de nv-00 |
| nv-15 Proyección de relaciones (grafo) | EP-098 | **retirado** | Decisión D-2: sin grafo. Las relaciones se resuelven con `$graphLookup`, `recommendations` y `saved` |
| nv-16 Cuenta, recuperación y sesiones | EP-006, 009, 010, 011 | **nv-04** | Subconjunto exacto. nv-04 ya los declaraba |
| nv-17 Administración de categorías | EP-014, 015 | **nv-06** | Subconjunto exacto |
| nv-18 Reacciones, guardados e indicadores | EP-036…040, 045, 046 | **nv-08** | Subconjunto exacto |
| nv-19 Experiencias, menciones y reputación | EP-041…044, 093…095 | **nv-08** | Subconjunto exacto |
| nv-20 Intereses y candidatos | EP-053…057 | **nv-09** | Subconjunto exacto |
| nv-21 Dispositivos y notificaciones push | EP-083…086 | **nv-12** | Subconjunto exacto |
| nv-22 Operación de datos y recuperación | — | **nv-14** | Operación e infraestructura comparten gates y runbook |
| nv-23 Landing y expediente | — | **nv-14** | nv-14 ya incluía landing y publicación |
| nv-24 Administración de confianza | EP-080, 081 | **nv-05** | Subconjunto exacto. La cola de revisión es la misma |
| nv-25 Consentimiento y políticas de contacto | EP-091, 092, 096, 097, 100 | **nv-06** y **nv-10** | Subconjunto exacto, repartido según el dueño del dato |

## Única operación retirada

**EP-098** — lectura del grafo de relaciones (nv-15).

El documento de requisitos lo define como *"opción propuesta"*, como *"proyección reconstruible"* (no fuente de verdad) con *"una API de lectura opcional"*, y exige *"comparar costo (…) antes de elegir"*. Hecha la comparación: AuraDB Free se auto-pausa por inactividad y se elimina si queda pausada ~30 días, con la evaluación el 2 de diciembre.

El mismo documento da la alternativa: *"MongoDB ofrece `$graphLookup` para ciertos recorridos"* y *"las menciones actuales continúan funcionando mediante REST y MongoDB"*.

**Ninguna función de la maqueta se pierde**: recomendaciones, guardados y relaciones por categoría y zona se resuelven con `recommendations`, `saved`, `$graphLookup` y el índice `2dsphere`.

## Qué cambia y qué no

**No cambia**: las 101 operaciones menos EP-098, las reglas de negocio, la maqueta, la trazabilidad FR → tarea → AC, ni el molde de spec/plan/task/gates.

**Cambia**: dónde vive cada operación, cuántos documentos hay que mantener en paralelo, y la arquitectura de despliegue (enmienda E-001).

## Orden de trabajo

**Principio: la plataforma se termina al principio, no al final.**

Toda la arquitectura, la nube y el CI-CD se cierran en la Fase 0 con un **esqueleto que ya llega a producción**: un solo endpoint (`/health/ready`) respondiendo por HTTPS, desde dos instancias en Azure, desplegado por la tubería automática, con observabilidad y las bases conectadas.

A partir de ahí, cada funcionalidad entra por un camino ya pavimentado. **Nadie vuelve a configurar arquitectura en noviembre**, cuando el tiempo se necesita para la app y la evidencia.

### Fase 0 — Esqueleto hasta producción (primero, sin excepción)

| Orden | Spec | Qué queda cerrado | Requisito |
|---|---|---|---|
| 1 | **nv-00** | Solución, módulos, capas, fronteras, 3 bases, Redis, host de dos roles | 14, 15 |
| 2 | **nv-02** | Contratos, convenciones de API y **matriz de versiones antes de desarrollar** | 11, 15 |
| 3 | **nv-14-A** | Azure: plan B1, **2 instancias**, SSL/HTTPS, Atlas, Redis, Blob + **CDN**, Application Insights, Terraform | 13, 15 |
| 4 | **nv-03** | CI-CD completo: build, pruebas, SAST, secretos, despliegue y **reversión** | 5, 7, 8 |

**Criterio de salida de la Fase 0** — se comprueba de una vez y no se vuelve a tocar:

- [ ] `https://…/health/ready` responde **200** desde Azure, con certificado válido.
- [ ] El portal muestra **2 instancias** activas del plan B1.
- [ ] Un `push` dispara la tubería y el cambio llega solo a producción.
- [ ] Las trazas aparecen en Application Insights.
- [ ] Una imagen de prueba se sirve desde el **CDN**.
- [ ] Existe la matriz de versiones y el procedimiento de reversión, probado una vez.

Con eso, **los requisitos de plataforma (8, 11, 13, 15) quedan cumplidos y demostrables desde el primer día**, no la última semana.

### Fase 1 — Funcionalidad (por el camino ya hecho)

| Orden | Spec | Nota |
|---|---|---|
| 5 | **nv-04** Identidad y sesión | Base de todo; conserva el código existente. Cierra logout real entre 2 instancias (requisito 10) |
| 6 | **nv-05** Moderación, medios y reportes | Antes que publicar: nada se persiste sin moderar (requisito 16) |
| 7 | **nv-06** Perfiles, catálogo y cobertura | |
| 8 | **nv-07** Búsqueda, mapa y feed | Cierra caché en los tres niveles (requisito 9) |
| 9 | **nv-08** Trabajos, experiencias y guardados | |
| 10 | **nv-09** Solicitudes e intereses | |
| 11 | **nv-10** Pedidos y privacidad de contacto | |
| 12 | **nv-11** Conversaciones y avisos en vivo | |
| 13 | **nv-12** Notificaciones y eliminación distribuida | Push (requisito 16) y borrado de cuenta |
| 14 | **nv-13** ML e IA generativa | Requisito 16 |

Cada spec entra con su CI ya funcionando y su despliegue automático. **Ninguna toca infraestructura.**

### Fase 2 — Cierre (solo contenido y evidencia)

| Orden | Spec | Nota |
|---|---|---|
| 15 | **nv-14-B** | Landing con aviso de privacidad, expediente de evaluación, script de base de datos, `.rar` de entrega |

Aquí **no hay arquitectura que configurar**: todo está en producción desde la Fase 0. Solo se redacta, se empaqueta y se verifica.

> `nv-14` se ejecuta en dos momentos a propósito: **14-A** es plataforma y va primero; **14-B** es landing y expediente y va al final. Es el único spec partido, y está partido porque mezclar ambas cosas es lo que empuja la infraestructura al final del calendario.

### Por qué este orden y no otro

| Si la infraestructura va al final | Con esqueleto primero |
|---|---|
| El código funciona en local y nadie sabe si despliega | Cada cambio se despliega desde el día 1 |
| Los problemas de nube aparecen en la última semana | Aparecen cuando hay tiempo de resolverlos |
| CI-CD se improvisa para cumplir el requisito 8 | CI-CD es la forma normal de trabajar |
| Las 2 instancias se activan sin probar que el estado compartido funciona | El logout entre instancias se prueba desde el principio |
| La evaluación del 2 de diciembre depende de un despliegue nuevo | Depende de uno que lleva semanas funcionando |

## Verificación de la consolidación

```bash
# Los 101 EP siguen declarados (100 activos + EP-098 retirado con motivo)
grep -rhoE "EP-[0-9]{3}" specs/arquitectura/nv-*/spec.md | sort -u | wc -l

# Ningún spec activo cita un spec consolidado como dependencia
grep -rn "nv-1[5-9]\|nv-2[0-5]" specs/arquitectura/nv-*/spec.md
```

## Estado

Propuesta. **No aplicar sin la revisión de Johann**, por tratarse de specs que él redactó. El bloqueante es el mismo de la enmienda E-001.
