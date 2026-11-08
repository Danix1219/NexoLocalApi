# Base física revisada por responsabilidad

> **Documento historico.** Describe la estructura de ocho microservicios que la **enmienda E-001** retiro el 2026-11-06. Ya no refleja lo que hay en disco: no existen `nexolocal.Api.slnx`, `services/` ni los hosts por servicio. Se conserva como evidencia del trabajo ejecutado y de por que se cambio. **Estado vigente:** [estructura-backend.md](estructura-backend.md) y [mapa de specs](../specs/arquitectura/README.md).


La solución original `nexolocal.Api.slnx` conserva G01 y contiene 28 proyectos, nueve hosts HTTP y un Worker. El usuario pidió revisar la arquitectura interna según el caso de uso; ya no se imponen cuatro capas a todos los servicios.

| Servicio | Arquitectura | Proyectos |
|---|---|---|
| S01 Identity | hexagonal | Domain, Application, Infrastructure, Api |
| S02 Directory | vertical-slices | Features, Adapters, Api |
| S03 Community | feature-modules | Modules, Adapters, Api |
| S04 Orders | ddd-hexagonal | Domain, Application, Infrastructure, Api |
| S05 Messaging | ports-adapters-realtime | Core, Adapters, Api |
| S06 Trust | evaluation-pipeline | Evaluation, Adapters, Api |
| S07 Notifications | event-driven-delivery | Delivery, Adapters, Api, Worker |
| S08 Relations | read-projections | Projections, Adapters, Api |

Los núcleos no dependen de transporte o proveedores. Los adaptadores implementan capacidades del núcleo; Api/Worker componen y no referencian otros servicios. S03 comparte ensamblado entre módulos: sus límites internos deberán comprobarse cuando haya código. S08 sigue condicional.

[Decisiones y justificación SDD](arquitectura/seleccion-arquitecturas.md). `architecture-selection.json` resume la decisión local; `scaffold.json` registra proyectos/hosts. El verificador mantiene una política explícita independiente del manifiesto de hosts.

## Validación desde esta raíz

```powershell
dotnet restore nexolocal.Api.slnx
dotnet build nexolocal.Api.slnx --no-restore -c Release
python -B scripts/check_architecture.py
python -B -m unittest discover -s tests/Architecture -p "test_*.py"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ScaffoldHosts.ps1
```

El smoke requiere puertos locales 5187–5195 libres; inicia diez procesos propios, comprueba 404 de nueve hosts HTTP vacíos y supervivencia del Worker, y los detiene en finally. No prueba reglas, entrega, persistencia o permisos funcionales.

## Patrones y límites

Las carpetas reservan capacidades de identidad, slices de directorio, módulos de comunidad, agregados de pedidos, mensajería durable, pipeline de confianza, entrega y proyecciones. Outbox, idempotencia, máquinas de estados y estrategias no están implementados. No hay endpoints, rutas proxy, DB/broker/proveedores ni paquetes NuGet nuevos. No se afirma una mejora de rendimiento sin medición.

Api y Worker tienen Dockerfiles propios con contexto raíz y referencias adaptadas a cada grafo. Ejemplo:

```powershell
docker build -f services/orders/src/Api/Dockerfile -t nexolocal-orders:local .
dotnet run --project services/notifications/src/Worker/NexoLocal.Notifications.Worker.csproj -c Release --no-build
```

Imágenes Docker pendientes de ensayo. La solución está consolidada en este checkout junto con el SDD. CI remoto, contratos y revisión humana siguen pendientes; no se crea otro repositorio ni publicación nueva. Se conserva copia de la estructura previa en artifacts/architecture-before-61436060a1ae42f8a306ecfd7de42ffb para reversión local; no es parte de la solución activa.
