# Registro operativo de decisiones backend

Fecha de preparacion: 2026-11-03. Pertenece a nv-00. La lista D01-D08 del [acuerdo compartido](../../../docs_nexo/SDD_ALINEACION.md) conserva su significado; este registro concreta el impacto backend, evidencia y salida de cada decision. No altera decisiones moviles ni ratifica proveedores. Al resolver una decision, actualizar ambas referencias en el mismo cambio si cambia su significado.

## Hechos y acuerdos que pueden reutilizarse

| Evidencia o acuerdo | Estado observado/documentado | Limite |
|---|---|---|
| Backend por dominios G01/S01-S07 | Constitucion y mapa vigentes; S08 condicionado | Arquitectura interna hexagonal y paquetes siguen propuesta |
| Flutter consume APIs | Decision de producto documentada | No acceso a bases del servidor |
| Plantilla de Visual Studio net10.0 | Inspeccion previa de csproj/Program/Dockerfile registrada | No build/producto aceptado; .NET/YARP no se ratifican por archivo existente |
| Repositorios Danix1219/NexoLocalApi y NexoLocalApp | Publicacion/reglas/CI registrados en informe de equipo | No se ha refrescado estado remoto en esta fase |
| feature→develop→release→main y reintegracion | Acuerdo explicito del usuario | Danix1219 designado como revisor provisional; revisor independiente del equipo se incorporara despues; no inventar aprobacion independiente |
| Jira y pagina Confluence obligatorios | Politica local preparada | Datos reales y activacion pospuestos por el usuario; no publicar estos cambios hasta completarlos |
| Codex y Claude Code | Herramientas acordadas | Esta fase la realiza Codex; no hay ejecucion Claude ni agentes delegados por afirmar roles |

## Decisiones abiertas y puerta que bloquean

| ID | Confirmado o disponible | Falta resolver | Responsable/ratificador | Puerta |
|---|---|---|---|---|
| D01 | Plantilla net10.0 y propuesta de servicios/capas | Ratificar runtime/SDK de equipo, YARP, MongoDB/Redis, eventos y versiones exactas necesarias para la primera vertical | Arquitectura/equipo por identificar | Ready tecnico del primer incremento ejecutable; no preparacion documental |
| D02 | Flutter definido y separacion API/cliente | Decisiones de estado/navegacion/SQLite/almacenamiento seguro en su contexto | Arquitectura movil por identificar | Ready movil; backend solo necesita contrato de frontera acordado |
| D03 | Requisito dos replicas/TLS/observabilidad/CDN | Nube, region, presupuesto, cuentas/roles y prueba de costo | Infraestructura/producto por identificar | Provisionamiento y aceptacion de entrega, no escritura de specs |
| D04 | OTP/moderacion forman parte del alcance | Proveedor, cuotas, credenciales seguras, TTL/limites y comportamiento ante fallo | Identidad/confianza por identificar | Integracion y aceptacion real; una simulacion no firma SMS/moderacion |
| D05 | Privacidad, consentimiento y borrado requeridos | Titular/contacto, retencion/backups y textos/estados de consentimiento | Producto/privacidad por identificar | Formalizar y aceptar flujos de datos sensibles; no inventar politicas |
| D06 | ML/GenAI requeridos por alcance academico | Caso, dueño, dataset/modelo/metricas/proveedor y evaluacion | Equipo ML/producto por identificar | nv-13; no obliga a implementar IA antes de identidad |
| D07 | URLs GitHub y flujo de ramas acordados | Integrantes/revisor real, carpeta compartida, Play y datos Jira/Confluence | Gestion/equipo por identificar; Danix1219 titular GitHub conocido | Publicacion conforme al acuerdo y paquete/canal movil |
| D08 | Documentos fuente y cobertura preparados | Evidencias de entrevistas, referencias docentes exactas/commit y responsables de ratificacion | Producto/docente/equipo por identificar | Cierre de criterios correspondientes; no afirmar revision de fuentes ausentes |

## Como registrar una resolucion

Crear un ADR en docs/decisions/ con ID libre cuando exista una decision concreta que resolver. Campos: ID D relacionado, contexto y fuente/revision, alternativas, propuesta, motivo, efectos en specs/contratos/datos/app/CI, riesgos, estado, decisor/revisor reales, fecha y evidencia de ratificacion. No escribir un nombre/fecha de aprobacion por haber generado el ADR. Ninguna opcion se vuelve elegida por encabezar una lista.

Si se difiere una decision: registrar motivo, alcance que SI puede continuar, puerta que permanece bloqueada, responsable pendiente y condicion de reapertura. Una excepcion ademas necesita mitigacion y vencimiento/condicion de caducidad ratificados; sin ellos no se considera autorizada. No abrir por excepcion acceso entre bases ni publicar secretos.

## Diferimiento existente

Jira/Confluence: usuario indica que proporcionara datos mas adelante. Puede continuar la preparacion documental local; no se inventan URLs/tickets, no se activa un check con configuracion vacia y no se publica el nuevo payload pendiente. Tarea de activacion nv-00 conserva estado pendiente. Este diferimiento no ratifica D01-D08 ni acepta funcionalidad.

## Encargo posterior de base fisica (2026-11-03)

El usuario confirma: "Crear carpetas y proyectos base, sin logica de negocio". Se implementa estructura tecnica net10.0 con SDK local 10.0.302 para cuatro proyectos Identity, conforme al target de la plantilla. Ese encargo autoriza el scaffold; no se registra como ratificacion de YARP, MongoDB/Redis, SMS, eventos, nube o diseño funcional. D01 conserva sus decisiones pendientes y Ready funcional sigue abierto. Codigo local fuera del checkout Git por ubicacion solicitada; consolidacion/publicacion aun no realizadas.

## Correccion explicita del usuario: G01 y solucion original (2026-11-03)

El usuario rechaza la solucion paralela y confirma que el proyecto existente nexolocal.Api es Gateway G01; cada microservicio tendra su host. Se conserva nexolocal.Api.slnx, G01 no referencia proyectos de Identity y S01 usa services/identity/src/{Domain,Application,Infrastructure,Api}. [Evidencia vigente](../inicio-backend/correccion-base-microservicios.md). Esta decision aclara estructura/rol, no ratifica proveedores o negocio.
